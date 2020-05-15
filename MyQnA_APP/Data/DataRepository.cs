using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using MyQnA_APP.Data.Models;
using Microsoft.Extensions.Hosting.Internal;
using static Dapper.SqlMapper;

namespace MyQnA_APP.Data
{

    /**
     * The methods in the data repository are responsible for reading data.
     */
    public class DataRepository : IDataRepository

    {

        private readonly string _connectionString;

        /**
         * The configuration parameter in the constructor gives us access to items within 
         * the appsettings.json file. The key we use when accessing the configuration object
         * is the path to the item we want from the appsettings.json file with colons being 
         * used to navigate fields in the JSON. 
         */
        public DataRepository(IConfiguration configuration) { 
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }


        public AnswerGetResponse GetAnswer(int answerId)
        {
            using (var connection = new SqlConnection(_connectionString)) {
                connection.Open(); 
                return connection.QueryFirstOrDefault<AnswerGetResponse>(
                    @"EXEC dbo.Answer_Get_ByAnswerId @AnswerId = @AnswerId", 
                    new { AnswerId = answerId }); 
            }
        }


        /**
         * Paramter values passed into a dapper query using an object with its 
         * property names matching the paramter names. Dapper will then create and execute
         * a paramterized query
         * 
         * Gets the question and gets answers of the questions
         */
        public QuestionGetSingleResponse GetQuestion(int questionId)
        { 
            using (var connection = new SqlConnection(_connectionString)) 
            { 
                connection.Open(); 
                using (GridReader results = connection.QueryMultiple(
                    @"EXEC dbo.Question_GetSingle   
                        @QuestionId = @QuestionId;      
                    EXEC dbo.Answer_Get_ByQuestionId        
                        @QuestionId = @QuestionId",
                    new { QuestionId = questionId })) 
                {
                    var question = results.Read<QuestionGetSingleResponse>()
                        .FirstOrDefault(); 
                    if (question != null) 
                    { 
                        question.Answers = results.Read<AnswerGetResponse>()
                            .ToList(); 
                    } 
                    return question;
                } 
            } 
        }

        public IEnumerable<QuestionGetManyResponse> GetQuestions()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.Query<QuestionGetManyResponse>(
                  @"EXEC dbo.Question_GetMany"
                );
            }
        }


        public IEnumerable<QuestionGetManyResponse> GetQuestionsBySearch(string search)
        {
            using (var connection = new SqlConnection(_connectionString)) { 
                connection.Open();
                return connection.Query<QuestionGetManyResponse>
                    (@"EXEC dbo.Question_GetMany_BySearch @Search = @Search",
                    new { Search = search }); }
        }

        public IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open(); return connection.Query<QuestionGetManyResponse>
                    ("EXEC dbo.Question_GetUnanswered");
            }
        }

        public bool QuestionExists(int questionId)
        {
            using (var connection = new SqlConnection(_connectionString)) {
                connection.Open(); return connection.QueryFirst<bool>(
                    @"EXEC dbo.Question_Exists @QuestionId = @QuestionId", 
                    new { QuestionId = questionId });
            }
        }


        /**
         * We have used a model class called QuestionPostRequest for Dapper to map
         * to the SQL paramters.
         */
        public QuestionGetSingleResponse PostQuestion(QuestionPostFullRequest question)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open(); var questionId = connection.QueryFirst<int>(
                    @"EXEC dbo.Question_Post 
                        @Title = @Title, @Content = @Content,
                        @UserId = @UserId, @UserName = @UserName,
                        @Created = @Created", 
                    question
                    );

                return GetQuestion(questionId);
            }
        }

        public QuestionGetSingleResponse PutQuestion(int questionId, QuestionPutRequest question)

        {

            using (var connection = new SqlConnection(_connectionString))

            {

                connection.Open();

                connection.Execute(

                  @"EXEC dbo.Question_Put 

        @QuestionId = @QuestionId, @Title = @Title, @Content = @Content",

                  new { QuestionId = questionId, question.Title, question.Content }

                );

                return GetQuestion(questionId);

            }

        }


        public void DeleteQuestion(int questionId) 
        { 
            using (var connection = new SqlConnection(_connectionString)) 
            { 
                connection.Open(); 
                connection.Execute(@"EXEC dbo.Question_Delete 
                    @QuestionId = @QuestionId", new { QuestionId = questionId }
                                   ); 
            }
        }

        public AnswerGetResponse PostAnswer(AnswerPostFullRequest answer) { 
            using (var connection = new SqlConnection(_connectionString)) 
            { connection.Open(); 
                return connection.QueryFirst<AnswerGetResponse>(@"EXEC dbo.Answer_Post 
                    @QuestionId = @QuestionId, @Content = @Content, 
                    @UserId = @UserId, @UserName = @UserName,
                    @Created = @Created", answer);
            } 
        }

        public IEnumerable<QuestionGetManyResponse> GetQuestionWithAnswers()
        {

            Console.WriteLine("========QUESTIONWITHASNWERS=============");
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var questionDictionary = new Dictionary<int, QuestionGetManyResponse>();
                return connection.Query<QuestionGetManyResponse, AnswerGetResponse, QuestionGetManyResponse>
                    ("EXEC dbo.Question_GetMany_WithAnswers", map: (q, a) => {
                        QuestionGetManyResponse question;
                        if (!questionDictionary.TryGetValue(q.QuestionId, out question))
                        {
                            question = q;
                            question.Answers = new List<AnswerGetResponse>();
                            Console.WriteLine("asdhjasbdhjasgdjhasgdj");
                            questionDictionary.Add(question.QuestionId, question);
                        }
                        question.Answers.Add(a);
                        return question;
                    }, splitOn: "QuestionId")
                    .Distinct()
                    .ToList();
            }
        }

        public IEnumerable<QuestionGetManyResponse> GetQuestionsBySearchWithPaging(string search, int pageNumber, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString)) 
            { 
                connection.Open(); 
                var parameters = new 
                { 
                    Search = search, PageNumber = pageNumber, PageSize = pageSize
                };
                return connection.Query<QuestionGetManyResponse>(
                    @"EXEC dbo.Question_GetMany_BySearch_WithPaging   
                        @Search = @Search,     
                        @PageNumber = @PageNumber,    
                        @PageSize = @PageSize",
                    parameters);
            }
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            { 
                await connection.OpenAsync(); 
                return await connection.QueryAsync<QuestionGetManyResponse>(
                    "EXEC dbo.Question_GetUnanswered"); 
            }
        }
    }
}
