using System;

namespace Form8snCore.Rendering
{
    public class RenderResultInfo
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public TimeSpan LoadingTime { get; set; }
        public TimeSpan OverallTime { get; set; }
        public TimeSpan CustomRenderTime { get; set; }
    }
}