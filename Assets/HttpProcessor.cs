using UnityEngine;
using System.Collections;

using System;
using System.IO;

using System.Net;
using System.Net.Sockets;

using System.Text;

public class HttpProcessor {

	public TcpClient socket;
	public UnityServer srv;

	public NetworkStream netStream;

	public string http_method;
	public string http_url;
	public string http_protocol_versionstring;
	public Hashtable httpHeadedrs = new Hashtable();

	private string error = null;

	public HttpProcessor(TcpClient s, UnityServer srv){
		this.socket = s;
		this.srv = srv;
	}

	public IEnumerator process(){
		netStream = socket.GetStream();

		// wait until request is parsed
		yield return srv.StartCoroutine(parseRequest());

		if(error == null){

			// wait until headers are parsed
			yield return srv.StartCoroutine(parseHeaders());

			if(error == null){

				switch(http_method){
				case "GET":
					yield return srv.StartCoroutine(handleGetRequest());
					break;
				case "POST":
					yield return srv.StartCoroutine(handlePostRequest());
					Debug.LogWarning("todo: controll stuff in unity");
					break;
				default:
					Debug.LogError("Unknown http_method " + http_method);
					error = "Unknown http_method " + http_method;
					break;
				}
			}
		}

		socket.Close();
		netStream.Close();
	}

	private IEnumerator parseRequest(){

		int startFrame = Time.frameCount;


		//todo: fix this fubar code. try netStream.Read as in post
		while(!netStream.DataAvailable){

			Debug.LogError("No data in request");
			error = "no data in request";
			yield break;

			//todo: wait a bit until timeout
			//yield return new WaitForSeconds(0.1f);
			//Debug.LogWarning("no request data. todo: timeout?");
		}

		// read a line from NetworkStream
		string request = null;
		yield return srv.StartCoroutine(readLine(netStream, value => request = value)); // danger??   http://answers.unity3d.com/questions/207733/can-coroutines-return-a-value.html
		//Debug.Log (" got request string: "+request);
		string[] tokens = request.Split(' ');
		if(tokens.Length != 3){
			Debug.LogError("Invalid http request line");
			error = "Invalid http request line";
			yield break;
		}
		http_method = tokens[0].ToUpper();
		http_url = tokens[1]; //todo: add index.html if it's a folder?
		http_protocol_versionstring = tokens[2];
	}

	private IEnumerator parseHeaders(){
		string line = null;
		//read lines until ""
		while(line != ""){
			yield return srv.StartCoroutine(readLine(netStream, value => line=value));
			if(line.Equals("")){
				//Debug.Log("Got headers");
			}
			else{
				int separator = line.IndexOf(':');
				if(separator == -1){
					Debug.LogError("invalid http header line: "+line);
					error = "invalid http header line: "+line;
					//todo: how to handle failure?
				}
				else{
					string name = line.Substring(0, separator);
					int pos = separator+1;
					while((pos < line.Length) && (line[pos]==' ')){
						pos++; //strip spaces
					}
					string value = line.Substring(pos, line.Length - pos);
					httpHeadedrs[name] = value;
				}
			}
		}
	}
	private IEnumerator handleGetRequest(){
		return srv.handleGetRequest(this);
	}
	private IEnumerator handlePostRequest(){
		byte[] readBuffer = new byte[1024];
		int bytesRead = netStream.Read(readBuffer, 0, readBuffer.Length);
		if(netStream.DataAvailable){
			Debug.LogWarning("didn't read all data");
		}

		return srv.handlePostRequest(this, readBuffer);
	}

	private IEnumerator readLine(NetworkStream stream, Action<string> line){
		StringBuilder currentLine = new StringBuilder("");
		int i;
		char c;
		bool lineRead = false;
		string finalLine = null;
		while(!lineRead){
			i = stream.ReadByte();
			if(i >= 0){
				c = (char)i;
				if(c == '\r' || c == '\n'){
					finalLine = currentLine.ToString();
					lineRead = true;
				}
				else{
					currentLine.Append(c);
				}
			}
			else{

				//yield return new WaitForSeconds(0.1f);
				//todo: better yield
				finalLine = currentLine.ToString();
				lineRead = true;
				Debug.LogWarning("no full line to read. todo: timeout?");
			}
		}
		line(finalLine);//pass retrieved result
		yield break;
	}
	public void writeSuccess(string content_type="text/html") {
		writeLine("HTTP/1.0 200 OK");            
		writeLine("Content-Type: " + content_type);
		writeLine("Connection: close");
		writeLine("");
	}
	public void writeFailure() {
		writeLine("HTTP/1.0 404 File not found");
		writeLine("Connection: close");
		writeLine("");
	}
	public void writeLine(string s){
		byte[] b = Encoding.UTF8.GetBytes(s+"\n");
		netStream.Write(b, 0, b.Length);
	}
}
