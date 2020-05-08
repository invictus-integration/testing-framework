using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Invictus.TestLibrary.LogicApps.UnitTests
{
    public class Utils
    {
        public static async Task<string> PostAsync(string uri, HttpContent content, Dictionary<string, string> headers)
        {
            using (var httpClient = new HttpClient())
            {
                headers.ToList().ForEach(x => httpClient.DefaultRequestHeaders.Add(x.Key, x.Value));
                var response = await httpClient.PostAsync(uri, content);

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
