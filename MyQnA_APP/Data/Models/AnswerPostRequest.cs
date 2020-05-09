﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyQnA_APP.Data.Models
{
    public class AnswerPostRequest { 
        public int QuestionId { get; set; } 
        public string Content { get; set; } 
        public string UserId { get; set; } 
        public string UserName { get; set; }
        public DateTime Created { get; set; }
    }
}