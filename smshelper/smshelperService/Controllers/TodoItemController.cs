using Microsoft.WindowsAzure.Mobile.Service;
using SmsHelperService.DataObjects;
using SmsHelperService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Twilio;

namespace SmsHelperService.Controllers
{
    public class TodoItemController : TableController<TodoItem>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            SmsHelperContext context = new SmsHelperContext();
            DomainManager = new EntityDomainManager<TodoItem>(context, Request, Services);
        }

        [Queryable(EnsureStableOrdering = false)]
        public IQueryable<TodoItem> GetAllTodoItems()
        {
            return Query().OrderByDescending(p => p.Sent);
        }


        [AllowAnonymous()]
        [Route("api/smscallback")]
        public async Task GetSmsCallback(string from, string to, string body)
        {
            this.Services.Log.Info("Recieved: " + from + " : " + body);
            TodoItem item = new TodoItem() { From = from, To = to, Text = body, Sent = DateTimeOffset.Now };
            TodoItem current = await InsertAsync(item);
        }

        public async Task<IHttpActionResult> PostTodoItem(TodoItem item)
        {
            this.Services.Log.Info("Sending: " + item.To + " : " + item.Text);
            // set our AccountSid and AuthToken
            string AccountSid = "AC3eaa75c5379382f3bcea087c922167cc";
            string AuthToken = "00a5b91b35e6a0c6626d49e245a55f89";
            item.From = "425-947-2529";
            item.Sent = DateTimeOffset.Now;


            // instantiate a new Twilio Rest Client
            var client = new TwilioRestClient(AccountSid, AuthToken);
            var message = await client.SendMessage(item.From, item.To, item.Text);
            this.Services.Log.Info("Sent message: " + message.Uri);

            TodoItem current = await InsertAsync(item);
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }
    }
}