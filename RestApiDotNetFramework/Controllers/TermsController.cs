using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using RestApiDotNetFramework.ErrorsHandling;
using RestApiDotNetFramework.Models;

namespace RestApiDotNetFramework.Controllers
{
    [EnableCors(origins: "http://127.0.0.1:5500", headers: "*", methods: "*")]
    public class TermsController : ApiController
    {
        readonly RestApiCsharpDbEntities db = new RestApiCsharpDbEntities();

        [HttpGet]
        [ActionName("ListOfTerms")]
        public IHttpActionResult GetList()
        {
            try
            {
                var listOfTerms = db.Terms.ToList();

                if (listOfTerms == null)
                    return ResponseMessage(ActionsExceptionsHandling.createResponseForException(HttpStatusCode.NotFound, "List of terms is empty"));

                return Ok(listOfTerms);
            }
            catch (Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }

        }

        [HttpGet]
        [ActionName("SingleTerm")]
        public IHttpActionResult GetTerm([FromUri] int id)
        {
            try
            {
                var termToFind = db.Terms.Find(id);

                if (termToFind == null)
                    return ResponseMessage(ActionsExceptionsHandling.createResponseForException(HttpStatusCode.NotFound, "Term with that id does not exist"));

                return Ok(termToFind);
            }
            catch (Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }
        }

        [HttpGet]
        [ActionName("SingleTermByCode")]
        public IHttpActionResult GetTermByCode([FromUri] int code) //wlasny routing
        {
            try
            {
                var termToFind = db.Terms.Find(code);

                if (termToFind == null)
                    return ResponseMessage(ActionsExceptionsHandling.createResponseForException(HttpStatusCode.NotFound, "Term with that code does not exist"));

                return Ok(termToFind);
            }
            catch (Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }
        }

        [HttpPost]
        [ActionName("Add")]
        public IHttpActionResult PostAdd([FromBody] Term newTerm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var termName = db.Terms.SingleOrDefault(t => t.Name == newTerm.Name);
                if(termName != null)
                    return ResponseMessage(ActionsExceptionsHandling.createResponseForException(HttpStatusCode.Conflict, "Term with the same name already exist"));

                //transakcja
                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        //Add term
                        db.Terms.Add(newTerm);
                        db.SaveChanges();

                        transaction.Commit();
                        return Ok("Add new term with id: " + newTerm.Id);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
                    }
                }
                //koniec transakcji

                //mozna dac poza transakcja
                //db.Terms.Add(newTerm);
                //db.SaveChanges();
                //return Ok(newTerm.Id);
            }
            catch(Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }
        }

        [HttpDelete]
        [ActionName("Delete")]
        public IHttpActionResult Delete([FromBody] int id)
        {
            try
            {
                //sprawdzenie czy dany termin istnieje
                var termToDelete = db.Terms.Find(id);

                if (termToDelete == null)
                    return ResponseMessage(ActionsExceptionsHandling.createResponseForException(HttpStatusCode.NotFound, "Term does not exist anymore"));

                db.Terms.Remove(termToDelete);
                db.SaveChanges();
                return Ok("Term has been deleted");
            }
            catch (Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }
            //var termToDelete = db.Terms.Find(id);
            //db.Terms.Remove(termToDelete);
            //db.SaveChanges();
            //return Ok();
        }

        [HttpPut]
        [ActionName("Modify")]
        public IHttpActionResult PutModify([FromBody] Term modifiedTerm, [FromUri] int termId)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                //sprawdzenie czy dany termin istnieje
                //ta linia powoduje bład; chyba przez to, że Find() dołącza znalezioną encję do kontekstu
                //var termToModify = db.Terms.Find(termId);
                //Attaching an entity of type 'RestApiDotNetFramework.Models.Term' failed because another entity of the same type already has the same primary key value.This can happen when using the 'Attach' method or setting the state of an entity to 'Unchanged' or 'Modified' if any entities in the graph have conflicting key values. This may be because some entities are new and have not yet received database - generated key values. In this case use the 'Add' method or the 'Added' entity state to track the graph and then set the state of non - new entities to 'Unchanged' or 'Modified' as appropriate.

                //if (termToModify == null)
                //    return ResponseMessage(ActionsExceptiosnHandling.createResponseForException(HttpStatusCode.NotFound, "You cannot modify term that does not exist"));

                db.Entry(modifiedTerm).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Ok("Term has been modified");
            }
            catch (Exception ex)
            {
                return ResponseMessage(ActionsExceptionsHandling.autoHandleException(ex));
            }
            //db.Entry(modifiedTerm).State = System.Data.Entity.EntityState.Modified;
            //db.SaveChanges();
            //return Ok("Term has been modified");
        }
    }
}
