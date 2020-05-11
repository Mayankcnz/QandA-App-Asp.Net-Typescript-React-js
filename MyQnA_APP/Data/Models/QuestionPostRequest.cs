using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // gives us access to the validation attributes

namespace MyQnA_APP.Data.Models
{
    public class QuestionPostRequest { 
        [Required]
        [StringLength(100)]
        public string Title { get; set; } 
        [Required(ErrorMessage ="Please include some content for this question")]
        public string Content { get; set; } 
    }
}
