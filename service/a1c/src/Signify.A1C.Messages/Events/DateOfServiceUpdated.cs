using NServiceBus;
using System;


namespace Signify.A1C.Messages.Events
{
	public sealed class DateOfServiceUpdated : IMessage
	{
		public int EvaluationId { get; set; }
		public DateTime DateOfService { get; set; }

		public DateOfServiceUpdated()
		{

		}

		public DateOfServiceUpdated(int evaluationId, DateTime dateOfService)
		{
			EvaluationId = evaluationId;
			DateOfService = dateOfService;
		}

		private bool Equals(DateOfServiceUpdated other)
		{
			return EvaluationId == other.EvaluationId && DateOfService.Equals(other.DateOfService);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == this.GetType() && Equals((DateOfServiceUpdated)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = EvaluationId.GetHashCode();
				hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"{nameof(EvaluationId)}: {EvaluationId}, {nameof(DateOfService)}: {DateOfService}";
		}
	}
}
