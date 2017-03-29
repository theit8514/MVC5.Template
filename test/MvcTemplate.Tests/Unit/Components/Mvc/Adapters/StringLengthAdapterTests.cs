using MvcTemplate.Components.Mvc;
using MvcTemplate.Resources.Form;
using MvcTemplate.Tests.Objects;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Xunit;

namespace MvcTemplate.Tests.Unit.Components.Mvc
{
    public class StringLengthAdapterTests
    {
        #region StringLengthAdapter(ModelMetadata metadata, ControllerContext context, StringLengthAttribute attribute)

        [Fact]
        public void StringLengthAdapter_SetsExceededErrorMessage()
        {
            ModelMetadata metadata = new DataAnnotationsModelMetadataProvider()
                .GetMetadataForProperty(null, typeof(AdaptersModel), "StringLength");
            StringLengthAttribute attribute = new StringLengthAttribute(128);

            // ReSharper disable once ObjectCreationAsStatement
            new StringLengthAdapter(metadata, new ControllerContext(), attribute);

            String expected = Validations.StringLength;
            String actual = attribute.ErrorMessage;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringLengthAdapter_SetsRangeErrorMessage()
        {
            StringLengthAttribute attribute = new StringLengthAttribute(128) { MinimumLength = 4 };
            ModelMetadata metadata = new DataAnnotationsModelMetadataProvider()
                .GetMetadataForProperty(null, typeof(AdaptersModel), "StringLength");

            // ReSharper disable once ObjectCreationAsStatement
            new StringLengthAdapter(metadata, new ControllerContext(), attribute);

            String expected = Validations.StringLengthRange;
            String actual = attribute.ErrorMessage;

            Assert.Equal(expected, actual);
        }

        #endregion
    }
}
