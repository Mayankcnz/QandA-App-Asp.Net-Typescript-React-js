using MyQnA_APP.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyQnA_APP.Data
{

    /**
     * A key difference of an asynchronous method is that it returns a Task of the type that will eventually be returned. 
     */
    public interface IDataRepository
    {
        IEnumerable<QuestionGetManyResponse> GetQuestions();
        IEnumerable<QuestionGetManyResponse> GetQuestionsBySearch(string search);
        Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync();
        
        IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions();
        QuestionGetSingleResponse GetQuestion(int questionId);
        bool QuestionExists(int questionId);
        AnswerGetResponse GetAnswer(int answerId);

        QuestionGetSingleResponse PostQuestion(QuestionPostFullRequest question);
        QuestionGetSingleResponse PutQuestion(int questionId, QuestionPutRequest question);
        void DeleteQuestion(int questionId);
        AnswerGetResponse PostAnswer(AnswerPostFullRequest answer);

        IEnumerable<QuestionGetManyResponse> GetQuestionWithAnswers();
        IEnumerable<QuestionGetManyResponse> GetQuestionsBySearchWithPaging(string search, int pageNumber, int pageSize);
    }
}
