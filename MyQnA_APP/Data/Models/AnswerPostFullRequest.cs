using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyQnA_APP.Data.Models
{
    public class AnswerPostFullRequest
    {

        [Required]
        public int? QuestionId { get; set; } // ? allows the property to have a null value as well as the declared type
                                             //T ? is the shortcut syntax for Nullable<T>
        [Required]
        public string Content { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Created { get; set; }
    }
}
