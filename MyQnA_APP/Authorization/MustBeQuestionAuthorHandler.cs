using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MyQnA_APP.Data;


namespace MyQnA_APP.Authorization
{
    /**
     * This inherits from the AuthorizationHandler class, which takes in the requirement 
     * it is handling as a generic parameter. We have injected the data repository
     * and the HTTP context into the class. 
     * 
     * At the moment, any authenticated user can update or delete questions. 
     * We are going to implement and use a custom authorization policy and use it 
     * to enforce that only the author of the question can do these operations. 
     */
    public class MustBeQuestionAuthorHandler : AuthorizationHandler<MustBeQuestionAuthorRequirement>
    {
        private readonly IDataRepository _dataRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MustBeQuestionAuthorHandler(IDataRepository dataRepository, IHttpContextAccessor httpContextAccessor)
        {
            _dataRepository = dataRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, MustBeQuestionAuthorRequirement requirement)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }

            var questionId = _httpContextAccessor.HttpContext.Request.RouteValues["questionId"];
            int questionIdAsInt = Convert.ToInt32(questionId);

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var question = await _dataRepository.GetQuestion(questionIdAsInt);
            if (question == null)
            {
                // let it through so the controller can return a 404
                context.Succeed(requirement);
                return;
            }

            if (question.UserId != userId)
            {
                context.Fail();
                return;
            }

            context.Succeed(requirement);
        }
    }
}
