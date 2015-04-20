using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CamTracker : MonoBehaviour {
	public GameObject Ball;
	private WebCamTexture webcamTexture;
	private Color32[] data = new Color32[921600];
	Texture2D texture;
	public bool Visualize = true;
	//public List<int> GoodPixels = new List<int>(307200);
	//OldMarble.RemoveAt(OldMarble.Count-1);OldMarble.Add(clone);
	bool[] GoodPixels = new bool[921600];
	int count = 0;
	int c_w, c_h;
	
	float[] Blobs = new float[765];
	
	public float brightnessThreshold = 0.9f;
	
	void Start() {
		//Debug.Log (WebCamTexture.devices[0].name);
		webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name,1280,720,60);
		webcamTexture.Play();
		data = new Color32[921600];
		texture = new Texture2D(1280,720);
		this.GetComponent<GUITexture>().texture = texture;
		Application.targetFrameRate = -1;
		c_w = webcamTexture.width;
		c_h = webcamTexture.height;
	}
	
	void Update() {
		count = 0;
		
		webcamTexture.GetPixels32(data);
		for(int i = 0; i<921600; i++){
			if(Gauntlet(data[i].r,data[i].g,data[i].b)){
				GoodPixels[i] = true;
				if(Visualize){
					data[i] = new Color32(0,0,0,255);
				}
			}else{
				//data[i] = new Color32(0,0,0,255);
			}
		}
		int biggest = 0;
		for(int i = 0; i<921600; i++){
			if(GoodPixels[i]){
				Vector3 Blob = BlobFind(i);
				if(Blob.z>20){
					Blobs[count*3] = Blob.x;
					Blobs[(count*3) + 1] = Blob.y;
					Blobs[(count*3) + 2] = Blob.z;
					if(Blob.z>Blobs[biggest+2]){
						biggest = count*3;
					}
					count++;
				}
			}
		}
		if(Blobs[biggest+2]>0f){
			Ball.transform.localPosition = new Vector3(5f-((Blobs[biggest]/Blobs[biggest+2])/64f),0,5f-((Blobs[biggest+1]/Blobs[biggest+2])/48f));
			Ball.transform.localScale = new Vector3(Mathf.Sqrt(Blobs[biggest+2]/Mathf.PI)*(1.562499f/75f),Mathf.Sqrt(Blobs[biggest+2]/Mathf.PI)*(2.083333f/75f),Mathf.Sqrt(Blobs[biggest+2]/Mathf.PI)/75f);
		}
		if(Visualize){
			texture.SetPixels32(data);
			texture.Apply(false);
		}
		for(int i = 0;i<765;i++){
			Blobs[i] = 0;
		}
		GoodPixels = new bool[921600];
	}
	
	Vector3 BlobFind(int Start){
		GoodPixels[Start] = false;
		//data[Start] = new Color32(0,0,0,255);  //USE THIS LINE TO VISUALIZE BLOBS
		Vector2 Pos = new Vector2(Start%c_w,Start/c_w);
		float Sum = 1;
		if( (Mathf.Floor(Start%c_w) !=0) && (GoodPixels[Start-1]) ){
			Vector3 nPos = BlobFind(Start-1);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start%c_w)!=1279) && (GoodPixels[Start+1]) ){
			Vector3 nPos = BlobFind(Start+1);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start/c_w) !=0) && (GoodPixels[Start-c_w]) ){
			Vector3 nPos = BlobFind(Start-c_w);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start/c_w)!=719) && (GoodPixels[Start+c_w]) ){
			Vector3 nPos = BlobFind(Start+c_w);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		return new Vector3(Pos.x,Pos.y,Sum);
	}
	
	void OnDestroy(){
		webcamTexture.Stop();
	}
	
	bool Gauntlet (int r, int g, int b){
		/*
		float brightness = ((float)r / 255.0f) * 0.3f + ((float)g / 255.0f) * 0.59f + ((float)b / 255.0f) * 0.11f;
		if(brightness>brightnessThreshold){
			return true;
		}else{
			return false;
		}
*/
		
		float h = 0;
		float max = Mathf.Max(r, Mathf.Max(g, b));
		if (max <= 0){
			return false;
		}
		float min = Mathf.Min(r, Mathf.Min(g, b));
		float dif = max - min;
		if(max>110){
			if(dif/max>.292){
				//if(dif/max>.592){
				if (max > min){
					if (g == max){
						h = (b - r) / dif * 60f + 120f;
					}else if (b == max){
						h = (r - g) / dif * 60f + 240f;
					}else if (b > g){
						h = (g - b) / dif * 60f + 360f;
					}else{
						h = (g - b) / dif * 60f;
					}
					if (h < 0){
						h += 360f;
					}
				}else{
					h = 0;
				}
				
				if (((h>=117.5f)&&(h<257.5f))){
					//if (((h>=167.5f)&&(h<207.5f))){
					return true;
				}else{
					return false;
				}
			}else{
				return false;
			}
		}else{
			return false;
		}
		
	}
	
}