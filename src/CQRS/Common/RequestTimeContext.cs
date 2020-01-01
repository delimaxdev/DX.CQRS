using System;

namespace DX.Cqrs.Common
{
    public class RequestTimeContext {
        public DateTime RequestTime { get; }

        public RequestTimeContext(DateTime requestTime)
            => RequestTime = requestTime;
    }
}