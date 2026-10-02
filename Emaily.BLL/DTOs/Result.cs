using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? ErrorMessage { get; }
        public int ErrorCode { get; }

        private Result(bool isSuccess, T? data, string? errorMessage, int errorCode)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        // دالة مساعدة لإنشاء نتيجة ناجحة بسهولة
        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, null, 200);
        }

        // دالة مساعدة لإنشاء نتيجة فاشلة بسهولة
        public static Result<T> Failure(string? errorMessage, int errorCode)
        {
            return new Result<T>(false, default, errorMessage ?? "unknown error", errorCode);
        }
    }
}
