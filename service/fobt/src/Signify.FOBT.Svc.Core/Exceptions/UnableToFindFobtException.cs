using System;

namespace Signify.FOBT.Svc.Core.Exceptions
{
    public class UnableToFindFobtException : Exception
    {
        public Guid OrderCorrelationId { get; set; }

        public string Barcode { get; }

        public int EvaluationId { get; }

        /// <summary>
        /// Unable to find FOBT record by the given <paramref name="orderCorrelationId"/>
        /// </summary>
        public UnableToFindFobtException(Guid orderCorrelationId, string barcode)
            : base($"Unable to find Fobt, for OrderCorrelationId {orderCorrelationId}")
        {
            OrderCorrelationId = orderCorrelationId;
            Barcode = barcode;
        }

        public UnableToFindFobtException(int evaluationId)
            : base($"Unable to find Fobt, for EvaluationId {evaluationId}")
        {
            EvaluationId = evaluationId;
        }

        public UnableToFindFobtException(string barcode)
           : base($"Unable to find Fobt, for Barcode {barcode}")
        {
            Barcode = barcode;
        }
    }
}
