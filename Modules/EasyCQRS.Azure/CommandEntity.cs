﻿using EasyCQRS.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCQRS.Azure
{
    [Table("Commands")]
    public class CommandEntity
    {        
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? ExecutedBy { get; set; }
        public DateTimeOffset ScheduledAt { get; set; }
        public DateTimeOffset? ExecutedAt { get; set; }
        public string Type { get; set; }
        public bool Executed { get; set; }
        public bool Success { get; set; }

        public byte[] Payload { get; set; }
        public string ErrorDescription { get; set; }
    }
}
