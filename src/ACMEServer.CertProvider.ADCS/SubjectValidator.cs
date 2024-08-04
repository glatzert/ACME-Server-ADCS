namespace Th11s.ACMEServer.CertProvider.ADCS
{
    internal class SubjectValidator
    {
        internal bool IsValid(CSRValidationContext validationContext)
        {
            // an empty subject is always acceptable
            if (validationContext.SubjectName == null)
                return true;

            // having no common name is always acceptable
            if (validationContext.CommonNames == null || validationContext.CommonNames.Count == 0)
                return true;

            // all common names need to be valid identifiers from the order
            foreach (var commonName in validationContext.CommonNames)
            {
                var matchingIdentifiers = validationContext.Identifiers
                    .Where(x => x.Value.Equals(commonName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingIdentifiers.Count == 0)
                    return false;

                foreach (var identifier in matchingIdentifiers)
                    validationContext.SetIdentifierToValid(identifier);
            }

            return true;
        }
    }
}
