using Microsoft.AspNetCore.Http;
using System.Net;

namespace StargateAPI.Controllers
{
    public static class ExceptionMapper
    {
        public static BaseResponse Map(Exception ex)
        {
            if (ex is BadHttpRequestException)
            {
                return new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = StatusCodes.Status400BadRequest
                };
            }

            // default to internal server error
            return new BaseResponse
            {
                Message = ex.Message,
                Success = false,
                ResponseCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
