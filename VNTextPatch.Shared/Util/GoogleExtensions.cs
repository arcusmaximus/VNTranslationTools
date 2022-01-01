using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Google;
using Google.Apis.Requests;

namespace VNTextPatch.Shared.Util
{
    internal static class GoogleExtensions
    {
        public static TResponse ExecuteRateLimited<TResponse>(this ClientServiceRequest<TResponse> request)
        {
            ExceptionDispatchInfo lastException = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return request.Execute();
                }
                catch (GoogleApiException ex) when (ex.Message.Contains("[rateLimitExceeded]"))
                {
                    lastException = ExceptionDispatchInfo.Capture(ex);
                    Thread.Sleep(5000);
                }
            }
            lastException?.Throw();
            throw new Exception("Request failed");
        }
    }
}
