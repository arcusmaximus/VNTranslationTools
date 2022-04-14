#pragma once

class ImeListener
{
public:
    static void Init();

    typedef void (__stdcall OnCompositionStarted_t)();
    static inline OnCompositionStarted_t* OnCompositionStarted{};

    typedef void (__stdcall OnCompositionEnded_t)(const std::wstring& text);
    static inline OnCompositionEnded_t* OnCompositionEnded{};

private:
    static HRESULT __stdcall CreateContextHook(
        ITfDocumentMgr* pDocumentMgr,
        TfClientId tidOwner,
        DWORD dwFlags,
        IUnknown* punk,
        ITfContext** ppic,
        TfEditCookie* pecTextStore
    );
    static inline decltype(CreateContextHook)* OriginalCreateContext{};

    class ContextOwnerCompositionSink : public ITfContextOwnerCompositionSink
    {
    public:
        ContextOwnerCompositionSink(ITfContextOwnerCompositionSink* pInnerSink);

        virtual HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
        virtual ULONG __stdcall AddRef() override;
        virtual ULONG __stdcall Release() override;
        virtual HRESULT __stdcall OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk) override;
        virtual HRESULT __stdcall OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew) override;
        virtual HRESULT __stdcall OnEndComposition(ITfCompositionView* pComposition) override;

    private:
        ComPtr<ITfContextOwnerCompositionSink> _innerSink;
        int _refcount{};
    };

    class TextEditSink : public ITfTextEditSink
    {
    public:
        virtual HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
        virtual ULONG __stdcall AddRef() override;
        virtual ULONG __stdcall Release() override;
        virtual HRESULT __stdcall OnEndEdit(ITfContext* pic, TfEditCookie ecReadOnly, ITfEditRecord* pEditRecord) override;

    private:
        int _refcount{};
    };

    static inline bool CompositionHasEnded{};
};
