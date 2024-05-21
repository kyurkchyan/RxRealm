namespace RxRealm.Core.IntegrationTests;

public static class Extensions
{
    public static async Task RetryWithExponentialBackoff(this Action action, int maxRetries = 5)
    {
        var retries = 0;
        var delay = TimeSpan.FromSeconds(1);

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception)
            {
                retries++;
                if (retries > maxRetries)
                {
                    throw;
                }
            }

            await Task.Delay(delay);
            delay = delay.Multiply(2);
        }
    }
}
