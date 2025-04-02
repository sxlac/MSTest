namespace Signify.A1C.Svc.Core.Sagas
{
    public sealed class Result
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public Result()
        {
        }

        public Result(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        private bool Equals(Result other)
        {
            return IsSuccess == other.IsSuccess && ErrorMessage == other.ErrorMessage;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Result)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (IsSuccess.GetHashCode() * 397) ^ (ErrorMessage != null ? ErrorMessage.GetHashCode() : 0);
            }
        }

        public static Result Create(bool isSuccess = true, string errorMessage = "")
            => new Result(isSuccess, errorMessage);
    }
}
