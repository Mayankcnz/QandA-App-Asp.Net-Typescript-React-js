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



        public QuestionsController(IDataRepository dataRepository, IHubContext<QuestionsHub> questionHubContext)

        {
            _dataRepository = dataRepository;
            _questionHubContext = questionHubContext;
          
        }



        [HttpGet]

        public IEnumerable<QuestionGetManyResponse> GetQuestions(string search, bool includeAnswers, int page = 1 , int pageSize = 20)

        {

            if (string.IsNullOrEmpty(search))

            {

                if(includeAnswers)
                {
                    return _dataRepository.GetQuestionWithAnswers();
                }else
                {
                    return _dataRepository.GetQuestions();
                }
           
                
            }
            else

            {
                return _dataRepository.GetQuestionsBySearchWithPaging(
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
        public ActionResult<QuestionGetSingleResponse> GetQuestion(int questionId)

        {

            var question = _dataRepository.GetQuestion(questionId);

            if (question == null)
            {
                return NotFound();
            }
            return question;

        }



        [HttpPost]

        [HttpPost]
        public ActionResult<QuestionGetSingleResponse> PostQuestion(QuestionPostRequest questionPostRequest)
        {
            var savedQuestion = _dataRepository.PostQuestion(new QuestionPostFullRequest
            {
                Title = questionPostRequest.Title,
                Content = questionPostRequest.Content,
                UserId = "1",
                UserName = "bob.test@test.com",
                Created = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetQuestion),
                new { questionId = savedQuestion.QuestionId },
                savedQuestion);
        }


        /**
         *Asp.Net core model minding will populate the questionPutRequest class
         * instance from the Http request body
         */
        [HttpPut("{questionID}")]
        public ActionResult<QuestionGetSingleResponse> PutQuestion(int questionID,
            QuestionPutRequest questionPutRequest)
        {

            // Console.WriteLine(questionPutRequest);

            var question =
                _dataRepository.GetQuestion(questionID);
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
                _dataRepository.PutQuestion(questionID, questionPutRequest);

            return savedQuestion;
        }

        [HttpDelete("{questionId}")]
        public ActionResult DeleteQuestion(int questionId)
        {

            var question = _dataRepository.GetQuestion(questionId);
            if (question == null)
            {
                return NotFound();
            }
            _dataRepository.DeleteQuestion(questionId);
            return NoContent();
        }

        /**
         * The method checks whether the question exists and returns a 404 HTTP status code if it doesn't.
         * The answer is then passed to the data repository to insert into the database.
         * The saved answer is returned from the data repository, which is returned in the response.
         */
        [HttpPost("answer")]
        public ActionResult<AnswerGetResponse> PostAnswer(AnswerPostRequest answerPostRequest)
        {
            var questionExists = _dataRepository.QuestionExists(answerPostRequest.QuestionId.Value);
            if (!questionExists)
            {
                return NotFound();
            }

            // mapping data from request model in the data respitory.
            // For big projects can also using AutoMapper
            var savedAnswer = _dataRepository.PostAnswer(new AnswerPostFullRequest
            {
                QuestionId = answerPostRequest.QuestionId.Value,
                Content = answerPostRequest.Content,
                UserId = "1",
                UserName = "bob.test@test.com",
                Created = DateTime.UtcNow
            });


            /**
             * We get access to the SignalR group through the Group method in the Clients property in the hub context
             * by passing in the group name. Remember that the group name is the word "Question", 
             * followed by a hyphen and then the question ID. Then, we use the SendAsync method to push 
             * the question with the new answer to all the clients in the group. A handler called ReceiveQuestion
             * will be invoked in the client, with the question being passed in as the parameter after 
             * we have got it from the data repository.
             */

            _questionHubContext.Clients.Group(
                $"Question-{answerPostRequest.QuestionId.Value}").
                SendAsync("ReceiveQuestion",
                _dataRepository.GetQuestion(answerPostRequest.QuestionId.Value));


            return savedAnswer;
        }


    }
}
