﻿using System;
using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Misc;

namespace ExpressiveAnnotations.ConditionalAttributes
{
    /// <summary>
    /// Validation attribute which indicates that annotated field is required when dependent field has appropriate value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RequiredIfAttribute : ValidationAttribute, IAttribute
    {
        private const string _defaultErrorMessage = "The {0} field is required by the following logic: {1}.";

        /// <summary>
        /// Gets or sets the name of dependent field from which runtime value is extracted.
        /// </summary>
        public string DependentProperty { get; set; }

        /// <summary>
        /// Gets or sets the expected value for dependent field (wildcard character * stands for any value). There is also possibility 
        /// of value runtime extraction from backing field, by providing its name [inside square brackets].
        /// </summary>
        public object TargetValue { get; set; }

        /// <summary>
        /// Gets or sets the relational operator describing relation between dependent field and target value. Available operators: 
        /// ==, !=, >, >=, &lt;, &lt;=. If this property is not provided, equality operator == is used by default.
        /// </summary>
        public string RelationalOperator { get; set; }

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
        /// Initializes a new instance of the <see cref="RequiredIfAttribute"/> class.
        /// </summary>
        public RequiredIfAttribute()
            : base(_defaultErrorMessage)
        {
            SensitiveComparisons = true;
            InvalidOnFalse = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredIfAttribute"/> class.
        /// </summary>
        /// <param name="dependentProperty">The name of dependent field from which runtime value is extracted.</param>
        /// <param name="targetValue">The expected value for dependent field (wildcard character * stands for any value). There is also possibility of value runtime extraction from backing field, by providing its name [inside square brackets].</param>
        /// <param name="relationalOperator">The relational operator describing relation between dependent field and target value. Available operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=. If this property is not provided, equality operator == is used by default.</param>
        /// <param name="sensitiveComparisons">Case sensitivity of string comparisons.</param>
        public RequiredIfAttribute(string dependentProperty, object targetValue, string relationalOperator = null, bool sensitiveComparisons = true, bool invalidOnFalse = true)
            : base(_defaultErrorMessage)
        {
            DependentProperty = dependentProperty;
            TargetValue = targetValue;
            RelationalOperator = relationalOperator;
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
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var internals = new AttributeInternals
            {
                DependentProperty = DependentProperty,
                TargetValue = TargetValue,
                RelationalOperator = RelationalOperator,
                SensitiveComparisons = SensitiveComparisons
            };

            var valid = !value.IsEmpty();
            
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
            var relationalExpression = MiscHelper.ComposeRelationalExpression(DependentProperty, TargetValue, RelationalOperator);
            var message = FormatErrorMessage(displayName, relationalExpression);

            return message;
        }
    }
}