using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace RestApiDotNetFramework.ErrorsHandling
{
    public static class ActionsExceptionsHandling
    {
        public static Exception getLastInnerException(Exception ex)
        {
            if (ex.InnerException != null)
                return getLastInnerException(ex.InnerException);
            else
                return ex;
        }

        public static HttpResponseMessage createResponseForException(HttpStatusCode status, string message)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(message),
                ReasonPhrase = Regex.Replace(message, @"\t|\n|\r", "") //w message tabulatory lub (|) znaki nowej linii zostaną zastąpione przez ""
            };
        }

        public static HttpResponseMessage autoHandleException(Exception ex)
        {
            var responseText = responseTextIfEntityValidationErrorsExist(ex);
            if (responseText != null)
                return createResponseForException(HttpStatusCode.BadRequest, responseText);

            ex = getLastInnerException(ex);

            var sqlException = ex as SqlException;
            if(sqlException != null)
            {
                switch(sqlException.Number)
                {
                    case -2:
                        return createResponseForException(HttpStatusCode.InternalServerError, "Connection timeout");
                    case 208:
                        return createResponseForException(HttpStatusCode.InternalServerError, "Data cannot be read from database - table structure error");
                    case 942:
                        return createResponseForException(HttpStatusCode.InternalServerError, "Database is offline");
                    case 4060:
                        return createResponseForException(HttpStatusCode.InternalServerError, "Invalid database");
                    case 18452:
                        return createResponseForException(HttpStatusCode.InternalServerError, "Wrong database login");
                    default:
                        Exception error = ex;
                        while (error.InnerException != null)
                        {
                            error = error.InnerException;
                        }

                        return createResponseForException(HttpStatusCode.InternalServerError, error.Message);
                }
            }
            else
            {
                return createResponseForException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public static string responseTextIfEntityValidationErrorsExist(Exception ex)
        {
            if (ex.GetType().FullName == "System.Data.Entity.Validation.DbEntityValidationException")
            {
                var validationErrors = (ex as System.Data.Entity.Validation.DbEntityValidationException).EntityValidationErrors;
                var responseMessage = "";
                foreach (var errorEntry in validationErrors)
                {
                    var entityName = errorEntry.Entry.Entity.GetType().Name.Substring(0, errorEntry.Entry.Entity.GetType().Name.IndexOf("_"));
                    responseMessage += "ENTITY: " + entityName + " ERRORS: ";

                    foreach (var error in errorEntry.ValidationErrors)
                    {
                        responseMessage += error.PropertyName + ": " + error.ErrorMessage + "; ";
                    }
                }

                return responseMessage.Substring(0, responseMessage.Length - 2);
            }
            else
                return null;
        }
    }
}