namespace ApiBase.AuthenticationAuthorization.Helper;

public class DevMailThrottleHelper
{
    private long _lastSentMs = long.MinValue / 2;

    public bool TryClaim(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return true;
        }

        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _lastSentMs);

        if (now - last < (long)interval.TotalMilliseconds)
            return false;

        return Interlocked.CompareExchange(ref _lastSentMs, now, last) == last;
    }
}