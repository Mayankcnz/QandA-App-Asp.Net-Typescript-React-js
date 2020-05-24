using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyQnA_APP.Data.Models;
using MyQnA_APP.Data;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR;
using MyQnA_APP.Hubs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MyQnA_APP.Controllers
{

    /**
     * The route attribute defines the path that our controller will handle. In our case
     * the path will be api/questions because [controller] is substituted with the name
     * of the controller minus the word controller
     * 
     * The ApiController attribute includes bevaiour such as automatic method
     * validation, which we'll take advantage.
     */
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;
        // The hubcontext interface allows us to interact with signalR clients
        private readonly IHubContext<QuestionsHub> _questionHubContext;
        private readonly IQuestionCache _cache;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _auth0UserInfo;


        public QuestionsController(IDataRepository dataRepository, IHubContext<QuestionsHub> questionHubContext, IQuestionCache questionCache, IHttpClientFactory clientFactory
            , IConfiguration configuration)

        { 
            _dataRepository = dataRepository;
            _questionHubContext = questionHubContext;
            _cache = questionCache;
            _clientFactory = clientFactory;
            _auth0UserInfo = $"{configuration["Auth0:Authority"]}userinfo";
          
        }

        [HttpGet]

        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestions(string search, bool includeAnswers, int page = 1 , int pageSize = 20)

        {

            if (string.IsNullOrEmpty(search))

            {

                if(includeAnswers)
                {
                    return await _dataRepository.GetQuestionsWithAnswers();
                }else
                {
                    return await _dataRepository.GetQuestions();
                } 
                
            }
            else

            {
                return await _dataRepository.GetQuestionsBySearchWithPaging(
                    search,
                    page,
                    pageSize);
            }

        }

        [HttpGet("unanswered")]
        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions()
        {
            
            return await _dataRepository.GetUnansweredQuestionsAsync();

        }

        [HttpGet("{questionId}")]
        public async Task<ActionResult<QuestionGetSingleResponse>> GetQuestion(int questionId)

        {

            var question = _cache.Get(questionId);

            if (question == null)
            {
                question = await _dataRepository.GetQuestion(questionId);
                if(question == null)
                {
                    return NotFound();
                }

                _cache.Set(question);
            }

            return question;

        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<QuestionGetSingleResponse>> PostQuestion(QuestionPostRequest questionPostRequest)
        {
            var savedQuestion = await _dataRepository.PostQuestion(new QuestionPostFullRequest
            {
                Title = questionPostRequest.Title,
                Content = questionPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = "bob.test@test.com",
                Created = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetQuestion),
                new { questionId = savedQuestion.QuestionId },
                savedQuestion);
        }


        /**
         *Asp.Net core model binding will populate the questionPutRequest class
         * instance from the Http request body
         */
        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpPut("{questionID}")]
        public async Task<ActionResult<QuestionGetSingleResponse>> PutQuestion(int questionID,
            QuestionPutRequest questionPutRequest)
        {

            // Console.WriteLine(questionPutRequest);

            var question =
                await _dataRepository.GetQuestion(questionID);
            if (question == null)
            {
                return NotFound();
            }

            questionPutRequest.Title = string.IsNullOrEmpty(questionPutRequest.Title) ?
                question.Title :
                questionPutRequest.Title;
            questionPutRequest.Content =
                string.IsNullOrEmpty(questionPutRequest.Content) ?
                question.Content :
                questionPutRequest.Content;

            var savedQuestion =
                await _dataRepository.PutQuestion(questionID, questionPutRequest);

            _cache.Remove(savedQuestion.QuestionId);
            return savedQuestion;
        }

        [Authorize(Policy ="MustBeQuestionAuthor")]
        [HttpDelete("{questionId}")]
        public async Task<ActionResult> DeleteQuestion(int questionId)
        {

            var question = await _dataRepository.GetQuestion(questionId);
            if (question == null)
            {
                return NotFound();
            }
            await _dataRepository.DeleteQuestion(questionId);
            _cache.Remove(questionId);
            return NoContent();
        }

        /**
         * The method checks whether the question exists and returns a 404 HTTP status code if it doesn't.
         * The answer is then passed to the data repository to insert into the database.
         * The saved answer is returned from the data repository, which is returned in the response.
         */
        [Authorize]
        [HttpPost("answer")]
        public async Task<ActionResult<AnswerGetResponse>> PostAnswer(AnswerPostRequest answerPostRequest)
        {
            var questionExists = await _dataRepository.QuestionExists(answerPostRequest.QuestionId.Value);
            if (!questionExists)
            {
                return NotFound();
            }

            // mapping data from request model in the data respitory.
            // For big projects can also using AutoMapper
            var savedAnswer = await _dataRepository.PostAnswer(new AnswerPostFullRequest
            {
                QuestionId = answerPostRequest.QuestionId.Value,
                Content = answerPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = "bob.test@test.com",
                Created = DateTime.UtcNow
            });

            _cache.Remove(answerPostRequest.QuestionId.Value);
            /**
             * We get access to the SignalR group through the Group method in the Clients property in the hub context
             * by passing in the group name. Remember that the group name is the word "Question", 
             * followed by a hyphen and then the question ID. Then, we use the SendAsync method to push 
             * the question with the new answer to all the clients in the group. A handler called ReceiveQuestion
             * will be invoked in the client, with the question being passed in as the parameter after 
             * we have got it from the data repository.
             */

            await _questionHubContext.Clients.Group(
                $"Question-{answerPostRequest.QuestionId.Value}").
                SendAsync("ReceiveQuestion",
                _dataRepository.GetQuestion(answerPostRequest.QuestionId.Value));


            return savedAnswer;
        }

        /**
         * We make GET HTTP request to the Auth0 User information endpoint
         * with the Authorization HTTP header from the current request to teh ASP.NET
         * Core backend. This HTTP header will contain the acess token that will give us acess
         * to the Auth0 endpoint.
         * 
         * If the reuqest is succesful, we parse the response body into the User model.
         * We used JSON serializer in .NET Core 3.0.
         * 
         */
        private async Task<string> GetUserName()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _auth0UserInfo);
            request.Headers.Add("Authorization", Request.Headers["Authorization"].First());

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return user.Name;
            }
            else
            {
                return "";
            }
        }


    }
}
