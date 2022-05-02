#pragma once

template<class T>
class ComPtr
{
private:
    T* _pInstance;

public:
    ComPtr()
    {
        _pInstance = nullptr;
    }

    ComPtr(T* pInstance)
    {
        _pInstance = pInstance;
        pInstance->AddRef();
    }

    ComPtr(const ComPtr& other)
    {
        _pInstance = other._pInstance;
        _pInstance->AddRef();
    }

    ComPtr(ComPtr&& other)
    {
        _pInstance = other._pInstance;
        other._pInstance = nullptr;
    }

    ~ComPtr()
    {
        if (_pInstance == nullptr)
            return;

        _pInstance->Release();
        _pInstance = nullptr;
    }

    template<class U>
    ComPtr<U> As()
    {
        ComPtr<U> pResult;
        if (_pInstance != nullptr)
            _pInstance->QueryInterface(&pResult);

        return pResult;
    }

    template<class U>
    bool Is()
    {
        return As<U>() != nullptr;
    }

    T* Get() const
    {
        return _pInstance;
    }

    operator T* () const
    {
        return _pInstance;
    }

    bool operator ==(T* pInstance) const
    {
        return _pInstance == pInstance;
    }

    bool operator !=(T* pInstance) const
    {
        return _pInstance != pInstance;
    }

    T** operator&()
    {
        return &_pInstance;
    }

    T& operator*()
    {
        return *_pInstance;
    }

    T* operator->()
    {
        return _pInstance;
    }

    void operator=(T* ptr)
    {
        if (_pInstance)
            _pInstance->Release();

        _pInstance = ptr;

        if (_pInstance)
            _pInstance->AddRef();
    }
};
