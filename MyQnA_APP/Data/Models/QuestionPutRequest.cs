using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyQnA_APP.Data.Models
{
    public class QuestionPutRequest
    { 
        [StringLength(100)]
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
