using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataAnnotationsValidator.Tests.Family;
using NUnit.Framework;

namespace DataAnnotationsValidator.Tests
{
    [TestFixture]
    public class DataAnnotationsValidatorTests
    {
        private IDataAnnotationsValidator _validator;

        [SetUp]
        public void Setup()
        {
            SaveValidationContextAttribute.SavedContexts.Clear();
            _validator = new DataAnnotationsValidator();
        }

        [Test]
        public void TryValidateObject_on_valid_parent_returns_no_errors()
        {
            // Arrange
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };

            // Act
            var result = _validator.TryValidateObject(parent, out var validationResults);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, validationResults.Count);
        }

        [Test]
        public void TryValidateObject_when_missing_required_properties_returns_errors()
        {
            var parent = new Parent
            {
                PropertyA = null,
                PropertyB = null
            };

            var result = _validator.TryValidateObject(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(2, validationResults.Count);
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "Parent PropertyA is required."));
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "Parent PropertyB is required."));
        }

        [Test]
        public void TryValidateObject_calls_IValidatableObject_method()
        {
            var parent = new Parent
            {
                PropertyA = 5,
                PropertyB = 6
            };

            var result = _validator.TryValidateObject(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);
            Assert.AreEqual("Parent PropertyA and PropertyB cannot add up to more than 10.", validationResults.First().ErrorMessage);
        }

        [Test]
        public void TryValidateObjectRecursive_returns_errors_when_child_class_has_invalid_properties()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = null,
                PropertyB = 5
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);
            Assert.AreEqual("Child PropertyA is required.", validationResults.First().ErrorMessage);
        }

        [Test]
        public void TryValidateObjectRecursive_ignored_errors_when_child_class_has_SkipRecursiveValidationProperty()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = 1,
                PropertyB = 1
            };
            parent.SkippedChild = new Child
            {
                PropertyA = null,
                PropertyB = 1
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsTrue(result);
            Assert.IsEmpty(validationResults);
        }

        [Test]
        public void TryValidateObjectRecursive_calls_IValidatableObject_method_on_child_class()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = 5,
                PropertyB = 6
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);
            Assert.AreEqual("Child PropertyA and PropertyB cannot add up to more than 10.", validationResults.First().ErrorMessage);
        }

        [Test]
        public void TryValidateObjectRecursive_returns_errors_when_grandchild_class_has_invalid_properties()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = 1,
                PropertyB = 1,
                GrandChildren = new[]
                {
                    new GrandChild
                    {
                        PropertyA = 11,
                        PropertyB = 11
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(2, validationResults.Count);
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "GrandChild PropertyA not within range."));
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "GrandChild PropertyB not within range."));
        }

        [Test]
        public void TryValidateObjectRecursive_passes_validation_context_items_to_all_validation_calls()
        {
            var parent = new Parent
            {
                Child = new Child
                {
                    GrandChildren = new[]
                    {
                        new GrandChild()
                    }
                }
            };

            var contextItems = new Dictionary<object, object>
            {
                { "key", 12345 }
            };

            _validator.TryValidateObjectRecursive(parent, out var validationResults, contextItems);

            Assert.AreEqual(3, SaveValidationContextAttribute.SavedContexts.Count, "Test expects 3 validated properties in the object graph to have a SaveValidationContextAttribute");
            Assert.That(SaveValidationContextAttribute.SavedContexts.Select(c => c.Items).All(items => items["key"] == contextItems["key"]));
        }

        [Test]
        public void TryValidateObject_calls_grandchild_IValidatableObject_method()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = 1,
                PropertyB = 1,
                GrandChildren = new[]
                {
                    new GrandChild
                    {
                        PropertyA = 5,
                        PropertyB = 6
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "GrandChild PropertyA and PropertyB cannot add up to more than 10."));
        }

        [Test]
        public void TryValidateObject_includes_errors_from_all_objects()
        {
            var parent = new Parent
            {
                PropertyA = 5,
                PropertyB = 6
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = 5,
                PropertyB = 6,
                GrandChildren = new[]
                {
                    new GrandChild
                    {
                        PropertyA = 5,
                        PropertyB = 6
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(3, validationResults.Count);
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "Parent PropertyA and PropertyB cannot add up to more than 10."));
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "Child PropertyA and PropertyB cannot add up to more than 10."));
            Assert.AreEqual(1, validationResults.Count(x => x.ErrorMessage == "GrandChild PropertyA and PropertyB cannot add up to more than 10."));
        }

        [Test]
        public void TryValidateObject_modifies_membernames_for_nested_properties()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            parent.Child = new Child
            {
                Parent = parent,
                PropertyA = null,
                PropertyB = 5,
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);
            Assert.AreEqual("Child PropertyA is required.", validationResults.First().ErrorMessage);
            Assert.AreEqual("Child.PropertyA", validationResults.First().MemberNames.First());
        }

        [Test]
        public void TryValidateObject_modifies_membernames_for_nested_and_enumeration_properties()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1,
                Child = new Child
                {
                    PropertyA = null,
                    PropertyB = 5,
                    GrandChildren = new List<GrandChild>
                    {
                        new GrandChild
                        {
                            PropertyA = 5,
                            PropertyB = 5
                        },
                        new GrandChild(),
                        new GrandChild
                        {
                            PropertyA = 7,
                            PropertyB = 8,
                        },
                        new GrandChild
                        {
                            PropertyA = 7,
                            PropertyB = 2,
                            PropertyC = 18
                        }
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(parent, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(6, validationResults.Count);

            var results = new List<ValidationResult>(validationResults);

            var result1 = results[0];
            Assert.AreEqual("The PropertyA field is required.", result1.ErrorMessage);
            Assert.AreEqual(1, result1.MemberNames.Count());
            Assert.AreEqual("Child.GrandChildren[1].PropertyA", result1.MemberNames.First());

            var result2 = results[1];
            Assert.AreEqual("The PropertyB field is required.", result2.ErrorMessage);
            Assert.AreEqual(1, result2.MemberNames.Count());
            Assert.AreEqual("Child.GrandChildren[1].PropertyB", result2.MemberNames.First());

            var result3 = results[2];
            Assert.AreEqual("GrandChild PropertyA and PropertyB cannot add up to more than 10.", result3.ErrorMessage);
            Assert.AreEqual(1, result3.MemberNames.Count());
            Assert.AreEqual("Child.GrandChildren[2]", result3.MemberNames.First());

            var result4 = results[3];
            Assert.AreEqual("GrandChild PropertyA and PropertyC cannot add up to more than 20.", result4.ErrorMessage);
            Assert.AreEqual(2, result4.MemberNames.Count());
            Assert.AreEqual("Child.GrandChildren[3].PropertyA", result4.MemberNames.First());
            Assert.AreEqual("Child.GrandChildren[3].PropertyC", result4.MemberNames.Last());

            var result5 = results[4];
            Assert.AreEqual("Child Parent is required.", result5.ErrorMessage);
            Assert.AreEqual(1, result5.MemberNames.Count());
            Assert.AreEqual("Child.Parent", result5.MemberNames.First());

            var result6 = results[5];
            Assert.AreEqual("Child PropertyA is required.", result6.ErrorMessage);
            Assert.AreEqual(1, result6.MemberNames.Count());
            Assert.AreEqual("Child.PropertyA", result6.MemberNames.First());
        }

        [Test]
        public void TryValidateObject_object_with_dictionary_does_not_fail()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            var classWithDictionary = new ClassWithDictionary
            {
                DataList = new List<Dictionary<string, Child>>
                {
                    new Dictionary<string, Child>
                    {
                        { "key",
                            new Child
                            {
                                Parent = parent,
                                PropertyA = 1,
                                PropertyB = 4
                            }
                        }
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(classWithDictionary, out var validationResults);

            Assert.IsTrue(result);
            Assert.IsEmpty(validationResults);
        }

        [Test]
        public void TryValidateObject_object_with_dictionary_does_fail()
        {
            var parent = new Parent
            {
                PropertyA = 1,
                PropertyB = 1
            };
            var classWithDictionary = new ClassWithDictionary
            {
                DataList = new List<Dictionary<string, Child>>
                {
                    new Dictionary<string, Child>
                    {
                        { "key0",
                            new Child
                            {
                                Parent = parent,
                                PropertyA = 1,
                                PropertyB = 1
                            }
                        },
                        { "key1",
                            new Child
                            {
                                Parent = parent,
                                PropertyA = 1,
                                PropertyB = 10
                            }
                        }
                    }
                }
            };

            var result = _validator.TryValidateObjectRecursive(classWithDictionary, out var validationResults);

            Assert.IsFalse(result);
            Assert.AreEqual(1, validationResults.Count);

            var results = new List<ValidationResult>(validationResults);

            var result1 = results[0];
            Assert.AreEqual("Child PropertyA and PropertyB cannot add up to more than 10.", result1.ErrorMessage);
            Assert.AreEqual(1, result1.MemberNames.Count());
            Assert.AreEqual(@"DataList[0][Index=1, Key=""key1""]", result1.MemberNames.First());
        }
    }
}
