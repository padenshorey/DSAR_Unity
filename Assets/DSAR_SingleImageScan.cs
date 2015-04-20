using UnityEngine;
using System.Collections;

public class DSAR_SingleImageScan : MonoBehaviour {

	public GUITexture p_cameraImage;
	public Texture2D p_cameraImageTexture;
	private Texture2D p_newTex;
	private Color[] p_cameraPixels;

	// Use this for initialization
	void Start () {
		p_cameraImage = this.gameObject.AddComponent<GUITexture> ();
		p_cameraImage.pixelInset = new Rect(0,0,Screen.width,Screen.height);
		p_cameraImage.texture = p_cameraImageTexture;
		p_newTex = new Texture2D (p_cameraImage.texture.width, p_cameraImage.texture.height);

		p_cameraPixels = p_cameraImageTexture.GetPixels ();
		for(int i=0; i<p_cameraPixels.Length; i++){
			if((p_cameraPixels[i].r + p_cameraPixels[i].g + p_cameraPixels[i].b)/3 > 240){
				p_cameraPixels[i] = new Color(0, 0, 0);
			}else{
				p_cameraPixels[i] = new Color(255, 255, 255);
			}
		}
		p_newTex.SetPixels (p_cameraPixels);
		p_newTex.Apply ();
		p_cameraImage.texture = p_newTex;
	}
	
	// Update is called once per frame
	void OnGUI () {

	}
}
