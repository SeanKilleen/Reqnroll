﻿using System;
using System.Collections.Generic;
using System.Text;
using Gherkin.CucumberMessages.Types;

namespace Reqnroll.Parser
{
    public interface ICucumberMessagesConverters
    {
        public GherkinDocument ConvertToCucumberMessagesGherkinDocument(ReqnrollDocument gherkinDocument);
        public Source ConvertToCucumberMessagesSource(ReqnrollDocument gherkinDocument);
        public IEnumerable<Pickle> ConvertToCucumberMessagesPickles(ReqnrollDocument gherkinDocument);
    }
}
