using System.Collections.Generic;

namespace VNTextPatch.Shared
{
    public interface ILoadWithParams
    {
        void LoadWithParams(ScriptLocation location, params object[] values);
    }
}
