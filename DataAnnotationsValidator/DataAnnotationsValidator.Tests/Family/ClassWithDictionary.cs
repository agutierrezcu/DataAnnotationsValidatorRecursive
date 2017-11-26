using System.Collections.Generic;

namespace DataAnnotationsValidator.Tests.Family
{
    public class ClassWithDictionary
    {
        public List<Dictionary<string, Child>> DataList { get; set; }
    }
}