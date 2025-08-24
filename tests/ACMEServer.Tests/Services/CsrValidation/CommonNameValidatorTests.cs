using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services.CsrValidation;

namespace Th11s.AcmeServer.Tests.Services.CsrValidation
{
    public class CommonNameValidatorTests
    {
        [Fact]
        public void NoCommonNames_IsValid()
        {
            var sut = new CommonNameValidator(NullLogger.Instance);

            var validationContext = new CsrValidationContext([], [], [], []);

            sut.ValidateCommonNamesAndIdentifierUsage(validationContext, [], [], []);
            Assert.True(validationContext.AreAllCommonNamesValid());
        }

        [Fact]
        public void CommonNameMatchesIdentifier_IsValid()
        {
            var identifier = new Identifier(IdentifierTypes.DNS, "valid.th11s.it");
            var commonNames = new string[] { "valid.th11s.it" };

            var sut = new CommonNameValidator(NullLogger.Instance);
            var validationContext = new CsrValidationContext([identifier], [], [], commonNames);

            sut.ValidateCommonNamesAndIdentifierUsage(validationContext, commonNames, [identifier], []);
            
            Assert.True(validationContext.AreAllCommonNamesValid());
            Assert.True(validationContext.IsIdentifierUsed(identifier));
        }

        [Fact]
        public void CommonNameDoesNotMatchIdentifier_IsInvalid()
        {
            var identifier = new Identifier(IdentifierTypes.DNS, "th11s.it");
            var commonNames = new string[] { "invalid.th11s.it" };

            var sut = new CommonNameValidator(NullLogger.Instance);
            var validationContext = new CsrValidationContext([identifier], [],[], commonNames);

            sut.ValidateCommonNamesAndIdentifierUsage(validationContext, commonNames, [new Identifier("dns", "example.com")], []);
            
            Assert.False(validationContext.AreAllCommonNamesValid());
            Assert.False(validationContext.IsIdentifierUsed(identifier));
        }
    }
}
