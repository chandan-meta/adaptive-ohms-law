using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace AdaptiveOhmsLaw
{
    // Service-neutral contract: POST {message,context,history}; reply {reply}.
    // The backend owns the tutor instructions, credentials and model selection.
    public sealed class HttpWattsonResponseProvider : IWattsonConversationProvider
    {
        [Serializable] class Request { public string message;public LearningContext context;public List<WattsonChatTurn> history; }
        [Serializable] class Response { public string reply = ""; public bool searched; public WattsonWebSource[] sources; public string sourcesPath; }
        public bool LastReplyUsedSearch {get;private set;}
        public WattsonWebSource[] LastSources {get;private set;} = Array.Empty<WattsonWebSource>();
        public string LastSourcesUrl {get;private set;}
        [Serializable] class ErrorResponse { public string error = ""; }
        readonly WattsonServiceSettings settings;
        public HttpWattsonResponseProvider(WattsonServiceSettings settings){this.settings=settings;}
        public Task<string> RespondAsync(string message,LearningContext context,CancellationToken cancellation)
            =>RespondAsync(message,context,Array.Empty<WattsonChatTurn>(),cancellation);
        public async Task<string> RespondAsync(string message,LearningContext context,IReadOnlyList<WattsonChatTurn> history,CancellationToken cancellation)
        {
            if(!settings.IsValidEndpoint)throw new InvalidOperationException("Configure a valid Wattson backend URL.");
            LastReplyUsedSearch=false;LastSources=Array.Empty<WattsonWebSource>();LastSourcesUrl=null;
            var body=new Request{message=message,context=context,history=new List<WattsonChatTurn>(history)};
            using(var request=new UnityWebRequest(settings.Endpoint,"POST")){
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=settings.timeoutSeconds;
                var operation=request.SendWebRequest();
                while(!operation.isDone){if(cancellation.IsCancellationRequested){request.Abort();cancellation.ThrowIfCancellationRequested();}await Task.Yield();}
                cancellation.ThrowIfCancellationRequested();
                if(request.result!=UnityWebRequest.Result.Success){
                    string code="";try{if(request.downloadHandler.text.Length<2000)code=JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text)?.error;}catch(ArgumentException){}
                    string reason=code=="provider_quota"?"Google's request quota is unavailable or exhausted. Check the API project's quota and billing, then retry.":code=="provider_access"?"Google rejected backend access. Check the server credential and API permissions.":code=="please_wait"?"Please wait a moment before asking another question.":code=="service_not_configured"?"The AI backend needs its Google credential configured.":"I couldn't reach the AI service. On desktop, make sure the Wattson backend is running, then retry.";
                    if(code=="provider_busy")reason="Google's AI service is temporarily busy. Please try again shortly.";
                    throw new WattsonServiceException(reason,code=="provider_busy");
                }
                if(request.downloadHandler.data.Length>64000)throw new InvalidOperationException("Wattson's response was too large.");
                var response=JsonUtility.FromJson<Response>(request.downloadHandler.text);
                if(response==null||string.IsNullOrWhiteSpace(response.reply))throw new InvalidOperationException("Wattson's service returned no reply.");
                LastReplyUsedSearch=response.searched;LastSources=response.sources??Array.Empty<WattsonWebSource>();
                if(response.sourcesPath!=null&&System.Text.RegularExpressions.Regex.IsMatch(response.sourcesPath,@"^/sources/[a-f0-9-]{36}$"))LastSourcesUrl=new Uri(new Uri(settings.Endpoint),response.sourcesPath).AbsoluteUri;
                return response.reply;
            }
        }
    }
    public sealed class WattsonServiceException : Exception { public bool TemporaryCapacityFailure {get;} public WattsonServiceException(string message,bool temporaryCapacityFailure=false):base(message){TemporaryCapacityFailure=temporaryCapacityFailure;} }
}

