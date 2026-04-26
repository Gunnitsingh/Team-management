using Microsoft.EntityFrameworkCore;


public class ConcurrencySaveHelper
{
    public async Task SaveWithConcurrencyRetry(Func<Task> action)
    {
        int retries = 3;

        while (retries > 0)
        {
            try
            {
                await action();
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                retries--;

                if (retries == 0)
                    throw;

                // reload entities
            }
        }
    }
}
