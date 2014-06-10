﻿using System;
using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Misc;

namespace ExpressiveAnnotations.ConditionalAttributes
{
    /// <summary>
    /// Validation attribute which indicates that annotated field is required when computed result of given logical expression is true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RequiredIfExpressionAttribute : ValidationAttribute, IExpressionAttribute
    {
        private const string _defaultErrorMessage = "The {0} field is required by the following logic: {1}.";
        private readonly RequiredAttribute _requiredAttribute = new RequiredAttribute();

        /// <summary>
        /// Gets or sets the logical expression based on which requirement condition is computed. 
        /// Available expression tokens: &amp;&amp;, ||, !, {, }, numbers and whitespaces.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Gets or sets the names of dependent fields from which runtime values are extracted.
        /// </summary>
        public string[] DependentProperties { get; set; }

        /// <summary>
        /// Gets or sets the expected values for corresponding dependent fields (wildcard character * stands for any value). There is also 
        /// possibility of values runtime extraction from backing fields, by providing their names [inside square brackets].
        /// </summary>
        public object[] TargetValues { get; set; }

        /// <summary>
        /// Gets or sets the relational operators describing relations between dependent fields and corresponding target values.
        /// Available operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=. If this property is not provided, equality operator == is used by default.
        /// </summary>
        public string[] RelationalOperators { get; set; }

        /// <summary>
        /// Gets or sets whether the string comparisons are case sensitive or not.
        /// </summary>
        public bool SensitiveComparisons { get; set; }

        /// <summary>
        /// Gets or set bool and bool? value types validation mode.
        /// Set to true to require true value and fail validation on false value.
        /// Set to false to require true and false and fail validation on null value.
        /// </summary>
        public bool InvalidOnFalse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredIfExpressionAttribute"/> class.
        /// </summary>
        public RequiredIfExpressionAttribute()
            : base(_defaultErrorMessage)
        {
            DependentProperties = new string[0];
            TargetValues = new object[0];
            RelationalOperators = new string[0];
            SensitiveComparisons = true;
            InvalidOnFalse = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredIfExpressionAttribute"/> class.
        /// </summary>
        /// <param name="expression">The logical expression based on which requirement condition is computed. Available expression tokens: &amp;&amp;, ||, !, {, }, numbers and whitespaces.</param>
        /// <param name="dependentProperties">The names of dependent fields from which runtime values are extracted.</param>
        /// <param name="targetValues">The expected values for corresponding dependent fields (wildcard character * stands for any value). There is also possibility of values runtime extraction from backing fields, by providing their names [inside square brackets].</param>
        /// <param name="relationalOperators">The relational operators describing relations between dependent fields and corresponding target values. Available operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=. If this property is not provided, equality operator == is used by default.</param>
        /// <param name="sensitiveComparisons">Case sensitivity of string comparisons.</param>
        public RequiredIfExpressionAttribute(string expression, string[] dependentProperties, object[] targetValues, string[] relationalOperators = null, bool sensitiveComparisons = true, bool invalidOnFalse = true)
            : base(_defaultErrorMessage)
        {
            Expression = expression;
            DependentProperties = dependentProperties ?? new string[0];
            TargetValues = targetValues ?? new object[0];
            RelationalOperators = relationalOperators ?? new string[0];
            SensitiveComparisons = sensitiveComparisons;
            InvalidOnFalse = invalidOnFalse;
        }

        /// <summary>
        /// Formats the error message.
        /// </summary>
        /// <param name="displayName">The user-visible name of the required field to include in the formatted message.</param>
        /// <param name="preprocessedExpression">The user-visible expression to include in the formatted message.</param>
        /// <returns>The localized message to present to the user.</returns>
        public string FormatErrorMessage(string displayName, string preprocessedExpression)
        {
            return string.Format(ErrorMessageString, displayName, preprocessedExpression);
        }

        /// <summary>
        /// Validates the specified value with respect to the current validation attribute.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">The context information about the validation operation.</param>
        /// <returns>
        /// An instance of the <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> class.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">validationContext;ValidationContext not provided.</exception>
        /// <exception cref="System.ArgumentException">
        /// Number of elements in DependentProperties and TargetValues must match.
        /// or
        /// Number of explicitly provided relational operators is incorrect.
        /// </exception>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var internals = new ExpressionAttributeInternals
            {
                Expression = Expression,
                DependentProperties = DependentProperties,
                TargetValues = TargetValues,
                RelationalOperators = RelationalOperators,
                SensitiveComparisons = SensitiveComparisons
            };

            var valid = _requiredAttribute.IsValid(value);
            
            if (valid && value is bool && InvalidOnFalse)
            { // validate for the true value of a radio element
                valid = (bool)value;
            }

            if (!valid && !internals.Verify(validationContext))
            { // return valid if the requirement condition not satisfied
                valid = true;
            }

            return valid ? ValidationResult.Success : new ValidationResult(GetValidationErrorMessage(validationContext.DisplayName));
        }

        private string GetValidationErrorMessage(string displayName)
        {
            var expression = MiscHelper.ComposeExpression(Expression, DependentProperties, TargetValues, RelationalOperators);
            var message = FormatErrorMessage(displayName, expression);

            return message;
        }
    }
}