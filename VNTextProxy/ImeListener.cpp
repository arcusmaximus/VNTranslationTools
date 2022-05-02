#include "pch.h"

// Raises events when IME compositions are started/ended.
// Normally this can be done way easier with WM_IME_ENDCOMPOSITION,
// but the Microsoft IME for Chinese apparently can't be bothered to send this,
// so we need to do a cumbersome workaround with the Text Services Framework instead.
//
// It seems the only way to be informed about TSF compositions starting/ending
// is through ITfContextOwnerCompositionSink. However, as the name implies,
// this event sink can only be registered by the context owner (when it creates the context).
// Because we're not the ones creating the context, we need to hook ITfDocumentMgr::CreateContext()
// so we can wrap the event sink in our own.

using namespace std;

void ImeListener::Init()
{
    CoInitialize(nullptr);

    {
        ComPtr<ITfThreadMgr> pThreadMgr;
        CoCreateInstance(CLSID_TF_ThreadMgr, nullptr, CLSCTX_INPROC_SERVER, IID_ITfThreadMgr, (void**)&pThreadMgr);

        TfClientId clientId;
        pThreadMgr->Activate(&clientId);

        {
            ComPtr<ITfDocumentMgr> pDocumentMgr;
            pThreadMgr->CreateDocumentMgr(&pDocumentMgr);
            void** pVtable = *(void***)pDocumentMgr.Get();
            OriginalCreateContext = (decltype(OriginalCreateContext))pVtable[3];
            {
                MemoryUnprotector unprotector(pVtable, sizeof(void*) * 9);
                pVtable[3] = CreateContextHook;
            }
        }

        pThreadMgr->Deactivate();
    }

    CoUninitialize();
}

HRESULT ImeListener::CreateContextHook(ITfDocumentMgr* pDocumentMgr, TfClientId tidOwner, DWORD dwFlags, IUnknown* punk, ITfContext** ppic, TfEditCookie* pecTextStore)
{
    ComPtr<IUnknown> pLinkedObj = punk;
    ComPtr<ITfContextOwnerCompositionSink> pOrigSink = pLinkedObj.As<ITfContextOwnerCompositionSink>();
    ComPtr<ITfContextOwnerCompositionSink> pNewSink = new ContextOwnerCompositionSink(pOrigSink);

    HRESULT result = OriginalCreateContext(pDocumentMgr, tidOwner, dwFlags, pNewSink, ppic, pecTextStore);
    if (result == S_OK)
    {
        ComPtr<ITfContext> pContext = *ppic;
        ComPtr<ITfSource> pContextEvents = pContext.As<ITfSource>();
        DWORD cookie;
        pContextEvents->AdviseSink(IID_ITfTextEditSink, new TextEditSink(), &cookie);
    }
    return result;
}

ImeListener::ContextOwnerCompositionSink::ContextOwnerCompositionSink(ITfContextOwnerCompositionSink* pInnerSink)
    : _innerSink(pInnerSink)
{
}

HRESULT ImeListener::ContextOwnerCompositionSink::QueryInterface(REFIID riid, void** ppvObject)
{
    if (riid == IID_ITfContextOwnerCompositionSink)
    {
        *ppvObject = this;
        AddRef();
        return S_OK;
    }
    return E_NOINTERFACE;
}

ULONG ImeListener::ContextOwnerCompositionSink::AddRef()
{
    return ++_refcount;
}

ULONG ImeListener::ContextOwnerCompositionSink::Release()
{
    int refcount = --_refcount;
    if (refcount == 0)
        delete this;

    return refcount;
}

HRESULT ImeListener::ContextOwnerCompositionSink::OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk)
{
    HRESULT result = _innerSink->OnStartComposition(pComposition, pfOk);
    if (result == S_OK && *pfOk && OnCompositionStarted != nullptr)
        OnCompositionStarted();

    return result;
}

HRESULT ImeListener::ContextOwnerCompositionSink::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
    return _innerSink->OnUpdateComposition(pComposition, pRangeNew);
}

HRESULT ImeListener::ContextOwnerCompositionSink::OnEndComposition(ITfCompositionView* pComposition)
{
    CompositionHasEnded = true;
    return _innerSink->OnEndComposition(pComposition);
}

HRESULT ImeListener::TextEditSink::QueryInterface(REFIID riid, void** ppvObject)
{
    if (riid == IID_ITfTextEditSink)
    {
        *ppvObject = this;
        AddRef();
        return S_OK;
    }
    return E_NOINTERFACE;
}

ULONG ImeListener::TextEditSink::AddRef(void)
{
    return ++_refcount;
}

ULONG ImeListener::TextEditSink::Release(void)
{
    int refcount = --_refcount;
    if (refcount == 0)
        delete this;

    return refcount;
}

HRESULT ImeListener::TextEditSink::OnEndEdit(ITfContext* pic, TfEditCookie ecReadOnly, ITfEditRecord* pEditRecord)
{
    if (!CompositionHasEnded)
        return S_OK;

    CompositionHasEnded = false;
    if (OnCompositionEnded == nullptr)
        return S_OK;

    ComPtr<IEnumTfRanges> pEnum;
    if (pEditRecord->GetTextAndPropertyUpdates(TF_GTP_INCL_TEXT, nullptr, 0, &pEnum) != S_OK)
        return S_OK;

    ComPtr<ITfRange> pRange;
    ULONG numRanges;
    if (pEnum->Next(1, &pRange, &numRanges) != S_OK || numRanges == 0)
        return S_OK;

    ComPtr<ITfRangeACP> pAcpRange = pRange.As<ITfRangeACP>();
    if (pAcpRange == nullptr)
        return S_OK;

    LONG startPos;
    LONG length;
    pAcpRange->GetExtent(&startPos, &length);

    wstring text;
    text.resize(length);
    pRange->GetText(ecReadOnly, 0, text.data(), text.size(), (ULONG*)&length);

    OnCompositionEnded(text);
    return S_OK;
}
