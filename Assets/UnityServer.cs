using UnityEngine;
using System.Collections;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Text;

public class UnityServer : MonoBehaviour {

	public int port = 8080;
	public IPAddress listen_ip = Dns.GetHostEntry("localhost").AddressList[0];

	//public float ListenDelay = 0.1f;
	private WaitForEndOfFrame wait = new WaitForEndOfFrame();

	private string local_public_html_url;

	void Start () {
		local_public_html_url = Application.dataPath;
		if(Application.isEditor){
			local_public_html_url += "/../public_html/";
		}
		else{
			local_public_html_url += "/public_html/";
		}
		local_public_html_url = Path.GetFullPath(local_public_html_url);
		local_public_html_url = local_public_html_url.Substring(0, local_public_html_url.Length-1);//remove last /
		local_public_html_url = "file://"+local_public_html_url;

		Debug.Log("Starting server. html files should be at "+local_public_html_url);

		StartCoroutine(listen(listen_ip, port));
	}

	// rotate just to check if it hangs...
	public float speed = 50f; 
	void Update () {
		transform.Rotate(Vector3.up*Time.deltaTime*speed);
	}

	private IEnumerator listen(IPAddress listen_ip, int port){
		TcpListener listener = new TcpListener( listen_ip, port );
		listener.Start();
		while(gameObject.activeSelf){

			if(listener.Pending()){
				TcpClient s = listener.AcceptTcpClient();
				//Debug.Log("got a client! create a HttpProcessor for him and start the process coroutine");
				HttpProcessor processor = new HttpProcessor(s, this);
				StartCoroutine(processor.process());
			}

			yield return wait;
			//yield return new WaitForSeconds(ListenDelay);
		}
	}

	public IEnumerator handleGetRequest(HttpProcessor p){
		Debug.Log("Handling request: "+p.http_url);

		string localFileUrl = p.http_url;
		if(localFileUrl.EndsWith("/")){
			localFileUrl = localFileUrl + "index.html";
		}
		localFileUrl = local_public_html_url + localFileUrl;

		WWW www = new WWW(localFileUrl);

		yield return www;
		if(!String.IsNullOrEmpty(www.error)){
			Debug.LogError(www.error);
			p.writeFailure();
		}
		else{
			if(p.http_url.EndsWith(".ico")){
				p.writeSuccess(content_type:"image/x-icon");
			}
			else if(p.http_url.EndsWith(".png")){
				p.writeSuccess(content_type:"image/png");
			}
			else if(p.http_url.EndsWith(".jpg") || p.http_url.EndsWith("jpeg")){
				p.writeSuccess(content_type:"image/jpeg");
			}
			else if(p.http_url.EndsWith(".js")){
				p.writeSuccess(content_type:"application/x-javascript");
			}
			else{
				p.writeSuccess();
			}
			p.netStream.Write(www.bytes, 0, www.bytes.Length);
		}
		//p.writeLine("<html><body><h1>unity web server</h1></br><img src=\"bear.png\" alt=\"Bear\">");
		yield break;
	}
	public IEnumerator handlePostRequest(HttpProcessor p, byte[] data){
		Debug.Log("Handling POST: "+p.http_url);
		Debug.Log(Encoding.UTF8.GetString(data));
		p.writeSuccess();
		p.writeLine("<h1>unity web server posted</h1></br>");
		yield break;
	}
}