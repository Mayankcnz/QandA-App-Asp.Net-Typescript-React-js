using Microsoft.Extensions.Caching.Memory;
using MyQnA_APP.Data.Models;


namespace MyQnA_APP.Data
{
    public class QuestionCache : IQuestionCache
    {
        private MemoryCache _cache { get; set; }
        public QuestionCache()
        {
            // create an instance of memory cache
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            });
        }

        /**
         * gives us a key for a cache item, which is the word
         * Question with a hyphen, followed by the questionID
         * We use the TryGetValue method within the memory cache to retrieve 
         * the cached question. So, null will be returned from our method 
         * if the question doesn't exist in the cache. 
         */
        private string GetCacheKey(int questionId) => $"Question-{questionId}";

        public QuestionGetSingleResponse Get(int questionId)
        {
            QuestionGetSingleResponse question;
            _cache.TryGetValue(GetCacheKey(questionId), out question);
            return question;
        }

        public void Set(QuestionGetSingleResponse question)
        {
            // specify the size of the question
            // This ties in with the size limitwe set on the cache so that the cache will 
            // start to remove questions from the cache when there are 100 questions in it 
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            _cache.Set(GetCacheKey(question.QuestionId), question, cacheEntryOptions);
        }

        public void Remove(int questionId)
        {
            _cache.Remove(GetCacheKey(questionId));
        }
    }
}
