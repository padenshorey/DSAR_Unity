using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;
using System.Linq;

public class DSAR_SceneManager : MonoBehaviour {

	public GUITexture BackgroundTexture;
	public GUITexture targetGUI;
	public Texture2D newTex;
	public WebCamDevice[] devices;
	public string backCamName;
	public WebCamTexture CameraTexture;
	public Texture2D CameraTextureStatic, pixelCheckTexture;
	public Color32[] data;
	public int counter = 900;
	public bool F_RGB, F_BW, F_BWC, F_OBEY, F_OBAMA, F_BLOB, F_BAW, F_FUN = false;
	public bool isCircle = true;
	public Color[] c;
	public Color32[] newTexColours;
	public Color newColor;
	public int timesCalled;
	public int c_w, c_h;
	private bool isRecursive = false;
	public GameObject blob;
	public List<GameObject> blobs = new List<GameObject>();
	public DSAR_Blob currentBlob;
	private float avBrightness = 200;
	
	private float frameRate = 30f;
	private float nextFrame = 0.0f;

	private int res_x = 640;
	private int res_y = 480;
	private bool[] GoodPixels;
	private float[] Blobs;
	private int b_count;
	private int count = 0;
	private List<Vector2> pixels_in_blob = new List<Vector2>();
	
	public GameObject testTexture;
	public bool useCamera = false;
	public bool firstScan = true;
	public GameObject BB_Point;

	private Vector2 topRight, topLeft, bottomRight, bottomLeft, rightTop, rightBottom, leftTop, leftBottom = new Vector2(0,0);
	
	public void Start(){

		//sets the proper frame rate value so that the math is correct
		frameRate = 1f / frameRate;

		if (useCamera) {
			nextFrame = Time.time + 2.0f;
			avBrightness = 250;

			BackgroundTexture = gameObject.AddComponent<GUITexture> ();
			BackgroundTexture.pixelInset = new Rect (0, 0, Screen.width, Screen.height);
			//set up camera
			devices = WebCamTexture.devices;
			backCamName = "";
			for (int i = 0; i < devices.Length; i++) {
				Debug.Log ("Device:" + devices [i].name + "IS FRONT FACING:" + devices [i].isFrontFacing);
			
				if (!devices [i].isFrontFacing) {
					backCamName = devices [i].name;
				}
			}

			CameraTexture = new WebCamTexture (backCamName, (int)BackgroundTexture.pixelInset.width, (int)BackgroundTexture.pixelInset.height, 30);
			CameraTexture.Play ();
			data = new Color32[CameraTexture.width * CameraTexture.height];
			BackgroundTexture.texture = CameraTexture;

			c_w = (int)CameraTexture.width;
			c_h = (int)CameraTexture.height;
		} else {
			avBrightness = 50;
			BackgroundTexture = gameObject.AddComponent<GUITexture> ();
			BackgroundTexture.pixelInset = new Rect (0, 0, 400, 400);

			CameraTextureStatic = (Texture2D)testTexture.GetComponent<GUITexture>().texture;

			data = new Color32[CameraTextureStatic.width*CameraTextureStatic.height];

			BackgroundTexture.texture = CameraTextureStatic;
			
			c_w = CameraTextureStatic.width;
			c_h = CameraTextureStatic.height;
		}

		newTex = new Texture2D (c_w, c_h);
		targetGUI.pixelInset = new Rect(0, 0, c_w, c_h);
		targetGUI.texture = newTex;

		newColor = new Color (255, 0, 255, 255);
		GoodPixels = new bool[data.Length];
		Blobs = new float[data.Length];

		pixelCheckTexture = new Texture2D (c_w, c_h);
		newTexColours = pixelCheckTexture.GetPixels32();

		this.GetComponent<GUITexture> ().enabled = false;
		Application.targetFrameRate = -1;

	}

	void Update() {
		if (!useCamera) {
			RunFilters ();
		} else {
			if (CameraTexture.didUpdateThisFrame)
				RunFilters();
		}
	}

	/**********************************************************************************/
	// M A I N   F U N C T I O N S
	/**********************************************************************************/

	private void RunFilters(){
		if(Time.time > nextFrame){
			
			nextFrame = Time.time + frameRate;
			
			if(useCamera)	CameraTexture.GetPixels32 (data);
			else 			data = CameraTextureStatic.GetPixels32();
			
			c = newTex.GetPixels();

			//filters
			if(F_BW)			FilterBW(c);
			else if(F_RGB)		FilterRGB(c);
			else if(F_BWC)		FilterBWC(c);
			else if(F_OBEY)		FilterOBEY(c);
			else if(F_FUN)		FilterFun(c);
			else if(F_BAW)		FilterBaW(c);
			else if(F_OBAMA)	FilterOBAMA(c);
			else if(F_BLOB)		BlobDetectionFilter();
			
			newTex.SetPixels(c);
			newTex.Apply();
		}
	}

	/**********************************************************************************/
	// W O R K I N G   F I L T E R 
	/**********************************************************************************/

	private void BlobDetectionFilter(){

		for (int i=0; i<data.Length; i++) {
			avBrightness += ((data[i].r + data[i].g + data[i].b)/3);
		}
		avBrightness /= c.Length;
		avBrightness *= 0.5f;

		//determine if pixels could be part of a blob
		for(int i=0; i<data.Length; i++){
			if((data[i].r + data[i].g + data[i].b)/3 < avBrightness){
				c[i] = Color.black;
				GoodPixels[i] = true;
			}else{
				c[i] = Color.white;
			}
		}

		for(int i = 0; i<GoodPixels.Length; i++){

			pixels_in_blob = new List<Vector2>();

			if(GoodPixels[i]){

				Vector3 Blob = BlobFind(i);
				if(Blob.z>100){
					Blobs[count*3] = Blob.x;
					Blobs[(count*3) + 1] = Blob.y;
					Blobs[(count*3) + 2] = Blob.z;

					if(firstScan){
						GameObject tempBlob = Instantiate(blob);
						blobs.Add(tempBlob);
						DSAR_Blob tb = tempBlob.GetComponent<DSAR_Blob>();
						tb.blobID = count;
						tb.pixels_in_blob = pixels_in_blob;
			
						//check for corners
						//float avWhitePixels = CheckCornerColours2(tb);
						//check radius lines from center of blob
						Vector4 sValues = CheckRadiusOfShape(new Vector2((int)(Blob.x/Blob.z), (int)(Blob.y/Blob.z)), pixels_in_blob.Count);

						//place bounding box dot on each vertice
						for(int j=0; j<tb.corner_pixels.Count; j++){
							GameObject bbpoint = Instantiate(BB_Point);
							tempBlob.GetComponent<DSAR_Blob>().corner_gos.Add(bbpoint);
							bbpoint.transform.parent = tempBlob.transform;
							bbpoint.GetComponent<GUITexture>().pixelInset = new Rect(tb.corner_pixels[j].x-10, tb.corner_pixels[j].y-10, 20, 20);
						}

						//tempBlob.GetComponent<GUIText>().text = sValues.x + ", " + sValues.y + ", " + sValues.z + ", " + sValues.w;

						//determine what the shape is based on the values obtained above
						if(sValues == new Vector4(0,0,0,0)){
							//no shape
							tempBlob.GetComponent<GUIText>().text = "";
						}else if(sValues.x == 12){
							//circle or square
							if(Mathf.Abs(sValues.z - sValues.w) < 1f){
								//circle or square, check corners
								if(tb.corner_pixels.Count >= 4){
									//square
									tempBlob.GetComponent<GUIText>().text = "Square";
								}else{
									//circle
									tempBlob.GetComponent<GUIText>().text = "Circle";
								}
							}else{
								//square
								tempBlob.GetComponent<GUIText>().text = "Square";
							}
						}else if(sValues.y == 1){
							//rectangle
							tempBlob.GetComponent<GUIText>().text = "Rectangle";
						}else{
							//triangle
							tempBlob.GetComponent<GUIText>().text = "Triangle";
						}

						tempBlob.name = tempBlob.GetComponent<GUIText>().text;

						//tempBlob.GetComponent<GUIText>().text = sValues.x + ", " + sValues.y + ", " + sValues.z + ", " + sValues.w;

		/*

						//Triangle
						//	s = 1, 2, 3
						//	w = 2
						//Square
						//	s = 9, 10
						//	s = 2, 3
						//Circle
						//	s = 6, 7, 8
						//	w = 2
						//Rectangle
						//	s = 2, 3
						//	w = 2, 3

						switch(tb.corner_pixels.Count){
						case 0:
							tempBlob.GetComponent<GUIText>().text = "Circle";
							break;
						case 1:
							tempBlob.GetComponent<GUIText>().text = "Circle";
							break;
						case 2:
							tempBlob.GetComponent<GUIText>().text = "Circle";
							break;
						case 3:
							tempBlob.GetComponent<GUIText>().text = "Triangle";
							break;
						case 4:
							tempBlob.GetComponent<GUIText>().text = "Rectangle";
							break;
						case 5:
							tempBlob.GetComponent<GUIText>().text = "Pentagon";
							break;
						case 6:
							tempBlob.GetComponent<GUIText>().text = "Hexagon";
							break;
						case 7:
							tempBlob.GetComponent<GUIText>().text = "Heptagon";
							break;
						case 8:
							tempBlob.GetComponent<GUIText>().text = "Octogon";
							break;
						case 9:
							tempBlob.GetComponent<GUIText>().text = "Nonagon";
							break;
						case 10:
							tempBlob.GetComponent<GUIText>().text = "Decagon";
							break;
						default:
							tempBlob.GetComponent<GUIText>().text = "???";
							break;
						}


						Vector2[] tempBB = new Vector2[tb.pixels_in_blob.Count];
						tb.numPixelsBB = chainHull_2D(tb.pixels_in_blob.ToArray(), tb.pixels_in_blob.Count, tempBB);

						for(int j=0; j<tb.numPixelsBB; j++){
							tb.pixels_in_bb.Add(tempBB[j]);
						}

						CheckCornerColours(tb);
						//CheckPointAngle(tb, 30);
						CheckPointDistance(tb, 0.001f);
						//int sValues = CheckRadiusOfShape(new Vector2((int)(Blob.x/Blob.z), (int)(Blob.y/Blob.z)));

						//place bounding box dot on each vertice
						for(int j=0; j<tb.pixels_in_bb.Count; j++){
							GameObject bbpoint = Instantiate(BB_Point);
							bbpoint.GetComponent<GUITexture>().pixelInset = new Rect(tb.pixels_in_bb[j].x-10, tb.pixels_in_bb[j].y-10, 20, 20);
						}

						tempBlob.GetComponent<GUIText>().text = "C: " + tb.pixels_in_bb.Count + ", S: " + sValues;

						//for(int k=0; k<tb.numPixelsBB-1; k++){
							//if(k==(tb.numPixelsBB-1))
							//	VectorLine.SetLine(Color.green, tb.pixels_in_bb[k], tb.pixels_in_bb[0]);
							//else
						//		VectorLine.SetLine(Color.green, tb.pixels_in_bb[k], tb.pixels_in_bb[k+1]);
						//}

						//VectorLine.SetLine (Color.green, topLeft, topRight);
						//VectorLine.SetLine (Color.green, topRight, rightTop);
						//VectorLine.SetLine (Color.green, rightTop, rightBottom);
						//VectorLine.SetLine (Color.green, rightBottom, bottomRight);
						//VectorLine.SetLine (Color.green, bottomRight, bottomLeft);
						//VectorLine.SetLine (Color.green, bottomLeft, leftBottom);
						//VectorLine.SetLine (Color.green, leftBottom, leftTop);
						//VectorLine.SetLine (Color.green, leftTop, topLeft); */
						
					}

					DSAR_Blob curBlobScript = blobs[count].GetComponent<DSAR_Blob>();
					curBlobScript.blob_velocity = new Vector2(curBlobScript.blob_position_center.x - (Blob.x/Blob.z), curBlobScript.blob_position_center.y - (Blob.y/Blob.z));
					curBlobScript.blob_position_center = new Vector2((int)(Blob.x/Blob.z), (int)(Blob.y/Blob.z));
					curBlobScript.blob_size = (int)Blob.z;

					GUITexture curBlobIndicator = blobs[count].GetComponent<GUITexture>();
					curBlobIndicator.pixelInset = new Rect ((int)(Blob.x/Blob.z)-10, (int)(Blob.y/Blob.z)-10, 20, 20);

					blobs[count].GetComponent<GUIText>().pixelOffset = new Vector2( (int)(Blob.x/Blob.z), (int)(Blob.y/Blob.z));

					count++;
				}
			}
		}

		pixelCheckTexture.SetPixels32 (newTexColours);
		pixelCheckTexture.Apply ();
		targetGUI.texture = pixelCheckTexture;

		for(int i = 0;i<data.Length;i++){
			Blobs[i] = 0;
		}

		GoodPixels = new bool[data.Length];
		count = 0;

		firstScan = false;
	}

	/**********************************************************************************/
	// S H A P E   C H E C K S 
	/**********************************************************************************/

	private Vector4 CheckRadiusOfShape(Vector2 centerPixel, int pib){
		int pixelNum = (int)((c_w*centerPixel.y)+centerPixel.x);

		//for the first test
		bool p_up, p_down, p_right, p_left;
		p_up = p_down = p_right = p_left = true;
		int mag_up, mag_down, mag_right, mag_left;
		mag_up = mag_down = mag_right = mag_left = 1;

		//for the second test
		bool p_ur, p_ul, p_dr, p_dl;
		p_ur = p_ul = p_dr = p_dl = true;
		int mag_ur, mag_ul, mag_dr, mag_dl;
		mag_ur = mag_ul = mag_dr = mag_dl = 1;

		while (p_up) {
			if(c[pixelNum + (c_w*mag_up)] == Color.black){
				mag_up++;
				newTexColours[pixelNum + (c_w*mag_up)] = Color.blue;
			}else{
				p_up = false;
				break;
			}
		}

		while (p_down) {
			if(c[pixelNum - (c_w*mag_down)] == Color.black){
				mag_down++;
				newTexColours[pixelNum - (c_w*mag_down)] = Color.blue;
			}else{
				p_down = false;
				break;
			}
		}

		while (p_right) {
			if(c[pixelNum + mag_right] == Color.black){
				mag_right++;
				newTexColours[pixelNum + mag_right] = Color.blue;
			}else{
				p_right = false;
				break;
			}
		}

		while (p_left) {
			if(c[pixelNum - mag_left] == Color.black){
				mag_left++;
				newTexColours[pixelNum - mag_left] = Color.blue;
			}else{
				p_left = false;
				break;
			}
		}

		//up-right
		while (p_ur) {
			if(c[pixelNum + mag_ur*c_w + mag_ur] == Color.black){
				mag_ur++;
				newTexColours[pixelNum + mag_ur*c_w + mag_ur] = Color.blue;
			}else{
				p_ur = false;
				break;
			}
		}

		//up-left
		while (p_ul) {
			if(c[pixelNum + mag_ul*c_w - mag_ul] == Color.black){
				mag_ul++;
				newTexColours[pixelNum + mag_ul*c_w - mag_ul] = Color.blue;
			}else{
				p_ul = false;
				break;
			}
		}

		//bottom-right
		while (p_dr) {
			if(c[pixelNum - mag_dr*c_w + mag_dr] == Color.black){
				mag_dr++;
				newTexColours[pixelNum - mag_dr*c_w + mag_dr] = Color.blue;
			}else{
				p_dr = false;
				break;
			}
		}

		//bottom-left
		while (p_dl) {
			if(c[pixelNum - mag_dl*c_w - mag_dl] == Color.black){
				mag_dl++;
				newTexColours[pixelNum - mag_dl*c_w - mag_dl] = Color.blue;
			}else{
				p_dl = false;
				break;
			}
		}

		float shapeRadThresh = 9;

		if (mag_up < shapeRadThresh ||
		    mag_down < shapeRadThresh ||
		    mag_left < shapeRadThresh ||
		    mag_right < shapeRadThresh ||
		    mag_ur < shapeRadThresh ||
		    mag_ul < shapeRadThresh ||
		    mag_dr < shapeRadThresh ||
		    mag_dl < shapeRadThresh) {

			return new Vector4(0, 0, 0, 0);

		} else {

			int[] values = {mag_up, mag_down, mag_left, mag_right};
			int[] values2 = {mag_ur, mag_ul, mag_dr, mag_dl};
			int similarValues1 = 0;
			int similarValues2 = 0;

			float av1, av2;
			av1 = av2 = 0;
		
			for (int j=0; j<values.Count(); j++) {
				av1 += values [j];
				av2 += values2 [j];
			}
		
			av1 /= values.Count ();
			av2 /= values2.Count ();
			//needed to compensate for pixel lengths (should be between 1.39 and 1.42)

		
			float bufferVal = (av1 + av2) / 2 * 0.2f;

			for (int j=0; j<(values.Count()-1); j++) {
				for (int k=j+1; k<values.Count(); k++) {
					//Debug.Log (values[j] + ", " + values[k]);
					if (Mathf.Abs (values [j] - values [k]) < bufferVal) {
						similarValues1++;
					}
				}
			}



			for (int j=0; j<(values2.Count()-1); j++) {
				for (int k=j+1; k<values2.Count(); k++) {
					//Debug.Log (values[j] + ", " + values[k]);
					if (Mathf.Abs (values2 [j] - values2 [k]) < bufferVal) {
						similarValues2++;
					}
				}
			}

			if(similarValues2>=6){
				av2 *= 1.4f;
			}

			bool tl_br, tr_bl;
			tl_br = tr_bl = false;
			int isRect = 0;

			if (Mathf.Abs (mag_ul - mag_dr) < 3f) {
				tl_br = true;
			}

			if (Mathf.Abs (mag_ur - mag_dl) < 3f) {
				tr_bl = true;
			}

			if (tl_br && tr_bl) {
				//is rectangle
				isRect = 1;
			}

			return new Vector4(similarValues1+similarValues2, isRect, av1, av2);
		}
	}

	private void CheckCornerColours(DSAR_Blob tb){
		for(int j=0; j<tb.pixels_in_bb.Count; j++){
			int pixelNum = (int)((c_w*tb.pixels_in_bb[j].y)+tb.pixels_in_bb[j].x);
			int whiteCount = 0;

			if(c[pixelNum] == Color.white)
				whiteCount++;
			if(c[pixelNum+1] == Color.white)
				whiteCount++;
			if(c[pixelNum-1] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w+1] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w-1] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w+1] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w-1] == Color.white)
				whiteCount++;

			//Debug.Log(whiteCount);
			//is corner if most pixels around are white
			if(whiteCount >= 5){
				//is corner
			}else{
				tb.pixels_in_bb[j] = new Vector2(-1,-1);
			}
		}

		//remove points that are too close together
		for(int p=tb.pixels_in_bb.Count-1; p>0; p--){
			if(tb.pixels_in_bb[p] == new Vector2(-1, -1)){
				tb.pixels_in_bb.Remove(tb.pixels_in_bb[p]);
			}
		}
	}

	private float CheckCornerColours2(DSAR_Blob tb){

		int totalWhiteCount = 0;
		int totalEdgeCount = 0;

		for(int j=0; j<tb.pixels_in_blob.Count; j++){
			int pixelNum = (int)((c_w*tb.pixels_in_blob[j].y)+tb.pixels_in_blob[j].x);
			int whiteCount = 0;
			
			if(c[pixelNum] == Color.white)
				whiteCount++;
			if(c[pixelNum+1] == Color.white)
				whiteCount++;
			if(c[pixelNum-1] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w+1] == Color.white)
				whiteCount++;
			if(c[pixelNum+c_w-1] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w+1] == Color.white)
				whiteCount++;
			if(c[pixelNum-c_w-1] == Color.white)
				whiteCount++;

			if(whiteCount > 0){
				totalWhiteCount += whiteCount;
				totalEdgeCount += 1;
			}

			//Debug.Log(whiteCount);
			//is corner if most pixels around are white

			if(whiteCount >= 5){
				//is corner
				tb.corner_pixels.Add(tb.pixels_in_blob[j]);
			}

		}

		return totalWhiteCount / totalEdgeCount;
	}

	private void CheckPointAngle(DSAR_Blob tb, float angleThreshold){
		//check angles between points
		float[] angles = new float[tb.pixels_in_bb.Count];
		
		for(int j=0; j < tb.pixels_in_bb.Count; j++){
			if(j!=0 && j!= tb.pixels_in_bb.Count-1){
				angles[j] = Vector2.Angle((tb.pixels_in_bb[j] - tb.pixels_in_bb[j-1]),(tb.pixels_in_bb[j] - tb.pixels_in_bb[j+1]));
			}else if(j==0){
				angles[j] = Vector2.Angle((tb.pixels_in_bb[j] - tb.pixels_in_bb[tb.pixels_in_bb.Count-1]),(tb.pixels_in_bb[j] - tb.pixels_in_bb[j+1]));
			}else if(j==tb.pixels_in_bb.Count){
				angles[j] = Vector2.Angle((tb.pixels_in_bb[j] - tb.pixels_in_bb[j-1]),(tb.pixels_in_bb[j] - tb.pixels_in_bb[0]));
			}
		}
		
		//remove points that are almost on the same vector
		for(int p=tb.pixels_in_bb.Count-1; p>0; p--){
			if(Mathf.Abs(angles[p]) < angleThreshold){
				tb.pixels_in_bb.Remove(tb.pixels_in_bb[p]);
			}
		}
	}
	
	private void CheckPointDistance(DSAR_Blob tb, float distanceThreshold){
		//check distances between remaining points
		for(int m=0; m<tb.corner_pixels.Count; m++){
			for(int n=0; n<tb.corner_pixels.Count; n++){
				if(m!=n){
					if(Vector2.Distance(tb.corner_pixels[m], tb.corner_pixels[n]) < (tb.pixels_in_blob.Count*distanceThreshold)){
						tb.corner_pixels[n] = new Vector2(-1, -1);
					}
				}
			}
		}
		
		//remove points that are too close together
		for(int p=tb.corner_pixels.Count-1; p>0; p--){
			if(tb.corner_pixels[p] == new Vector2(-1, -1)){
				tb.corner_pixels.Remove(tb.corner_pixels[p]);
			}
		}
	}
	
	private void CheckBoundingBox(Vector2 currentPixel){
		if (currentPixel.y >= topLeft.y && currentPixel.y >= topRight.y) {
			topLeft.y = topRight.y = currentPixel.y;

			if(Mathf.Abs(currentPixel.x - topLeft.x) < Mathf.Abs(currentPixel.x - topRight.x)){
				topLeft = currentPixel;
			}else{
				topRight = currentPixel;
			}
		}

		if (currentPixel.y <= bottomLeft.y && currentPixel.y <= bottomRight.y) {
			bottomLeft.y = bottomRight.y = currentPixel.y;

			if(Mathf.Abs(currentPixel.x - bottomLeft.x) < Mathf.Abs(currentPixel.x - bottomRight.x)){
				bottomLeft = currentPixel;
			}else{
				bottomRight = currentPixel;
			}
		}

		if (currentPixel.x >= rightTop.x || currentPixel.x >= rightBottom.x) {
			if(currentPixel.y >= rightTop.y){
				rightTop = currentPixel;
			}else if(currentPixel.y <= rightBottom.y){
				rightBottom = currentPixel;
			}
		}

		if (currentPixel.x <= leftTop.x || currentPixel.x <= leftBottom.x) {
			if(currentPixel.y >= leftTop.y){
				leftTop = currentPixel;
			}else if(currentPixel.y <= leftBottom.y){
				leftBottom = currentPixel;
			}
		}
	}

	/**********************************************************************************/
	// R E C U R S I O N   F U N C T I O N S
	/**********************************************************************************/
	
	private Vector3 BlobFind(int Start){

		GoodPixels[Start] = false;

		Vector2 Pos = new Vector2(Start%c_w,Start/c_w);

		pixels_in_blob.Add (Pos);

		//CheckBoundingBox (Pos);

		float Sum = 1;
		if( (Mathf.Floor(Start%c_w) !=0) && (GoodPixels[Start-1]) ){
			Vector3 nPos = BlobFind(Start-1);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start%c_w)!=c_w-1) && (GoodPixels[Start+1]) ){
			Vector3 nPos = BlobFind(Start+1);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start/c_w) !=0) && (GoodPixels[Start-c_w]) ){
			Vector3 nPos = BlobFind(Start-c_w);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		if( (Mathf.Floor(Start/c_w)!=c_h-1) && (GoodPixels[Start+c_w]) ){
			Vector3 nPos = BlobFind(Start+c_w);
			Pos = new Vector2(Pos.x+nPos.x,Pos.y+nPos.y);
			Sum += nPos.z;
		}
		return new Vector3(Pos.x,Pos.y,Sum);
	}

	public void FloodFill(int i){
		// 1. If target-color is equal to replacement-color, return.
		if (c [i] == newColor)
			return;
		// 2. If the color of node is not equal to target-color, return.
		if (!isBlack (c [i]))
			return;
		// 3. Set the color of node to replacement-color.
		c [i] = newColor;
		// 4. 	Perform Flood-fill (one step to the west of node, target-color, replacement-color).
		if (i % c_w != 0) {
			FloodFill (i - 1);
		}
		//		Perform Flood-fill (one step to the east of node, target-color, replacement-color).
		if ((i + 1) % c_w != 0 && i < (c_w * c_h)) {
			FloodFill (i + 1);
		}
		//		Perform Flood-fill (one step to the north of node, target-color, replacement-color).
		if (i + c_w < c.Length) {
			FloodFill (i + c_w);
		}
		//		Perform Flood-fill (one step to the south of node, target-color, replacement-color).
		if (i - c_w >= 0) {
			FloodFill (i - c_w);
		}
		//5. Return.
		return;
	}
	

	public bool solveShapes(int i){
		//continue only of the cell is black
		if (!isBlack (c [i])) {
			return false;
		}
		
		//first time called for new blob
		if (!isRecursive) {
			isRecursive = true;
			//GameObject tempBlob = Instantiate(blob);
			blobs.Add(blob);
			currentBlob = blobs [blobs.Count - 1].GetComponent<DSAR_Blob> ();
			b_count = 0;

		}

		b_count++;
		// commented out because its interfering
		//currentBlob.pixels_in_blob.Add (i);

		//check above pixel
		if (i + c_w < c.Length) {
			if(!isWhite (c[i+c_w])){
				c[i] = newColor;
				if(isBlack (c[i+c_w]))
					solveShapes(i+c_w);
				return true;
			}
		}

		//check left pixel
		if (i % c_w != 0) {
			if (!isWhite (c [i - 1])) {
				c [i] = newColor;
				if(isBlack (c [i - 1]))
					solveShapes(i-1);
				return true;
			}
		}

		//check right pixel
		if ((i+1) % c_w != 0 && i < (c_w*c_h)) {
			if (!isWhite (c [i + 1])) {
				c [i] = newColor;
				if(isBlack (c [i + 1]))
					solveShapes(i+1);
				return true;
			}
		}
		
		//check below pixel
		if (i - c_w >= 0) {
			if(!isWhite (c[i-c_w])){
				c[i] = newColor;
				if(isBlack (c[i-c_w]))
					solveShapes(i-c_w);
				return true;
			}
		}

		return false;
	}

	/**********************************************************************************/
	// C O L O U R   C H E C K E R S
	/**********************************************************************************/

	public bool isWhite(Color c){
		if (c.r == 255 && c.g == 255 && c.b == 255)
			return true;
		else
			return false;
	}

	public bool isBlack(Color c){
		if (c.r == 0 && c.g == 0 && c.b == 0)
			return true;
		else
			return false;
	}

	public bool isBlackOrWhite(Color c){
		if ((c.r == 0 && c.g == 0 && c.b == 0) || ((c.r == 255 && c.g == 255 && c.b == 255)))
			return true;
		else
			return false;
	}


	/**********************************************************************************/
	// F I L T E R S
	/**********************************************************************************/

	void FilterRGB(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;

		for(int i=0; i<data.Length; i++){
			/*if((data[i].r + data[i].g + data[i].b)/3 > 100){
					c[i] = data[i];
				}else{
					c[i] = new Color(255,255,255);
				}
				c[i].a = 255;*/
			
			int r, g, b = 0;
			r = data[i].r;
			g = data[i].g;
			b = data[i].b;
			
			if(r > g && r > b){
				c[i] = new Color(255,0,0,255);
			}else if(b > r && b > g){
				c[i] = new Color(0,255,0,255);
			}else{
				c[i] = new Color(0,0,255,255);
			}
		}
	}

	void FilterBWC(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;
		
		float avBrightness = 0;
		for (int i=0; i<data.Length; i++) {
			avBrightness += ((data[i].r + data[i].g + data[i].b)/3);
		}
		avBrightness /= c.Length;
		for(int i=0; i<data.Length; i++){
			if((data[i].r + data[i].g + data[i].b)/3 > avBrightness){
				//bright
				c[i] = new Color(255,255,255);
			}else{
				//dark
				c[i] = new Color(0,0,0);
			}
			c[i].a = 255;
		}
	}

	void FilterBaW(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;

		for(int i=0; i<data.Length; i++){
			float col = data[i].r + data[i].g + data[i].b;
			col /= 3;
			c[i] = new Color(col/255, col/255, col/255);
		}
	}

	void FilterFun(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;

		//Debug.Log ("RGB: (" + data[500].r + ", " + data[500].g + ", " + data[500].b + ")");

		for(int i=0; i<data.Length; i++){
			float col = data[i].r + data[i].g + data[i].b;
			col /= 3;
			if(col > 120)
				c[i] = new Color(255, col/255, col/255);
			else
				c[i] = new Color(col/255, col/255, col/255);
		}
	}
	
	void FilterOBEY(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;
		
		float avBrightness = 0;
		for (int i=0; i<data.Length; i++) {
			avBrightness += ((data[i].r + data[i].g + data[i].b)/3);
		}
		avBrightness /= c.Length;
		float thresh = 0.4f;
		float maxB = avBrightness + (avBrightness*thresh);
		float minB = avBrightness - (avBrightness*thresh);
		
		for(int i=0; i<data.Length; i++){
			float curAv = (data[i].r + data[i].g + data[i].b)/3;
			if(curAv > maxB){
				c[i] = new Color(0,0,0);
			}else if(curAv < minB){
				c[i] = new Color(252f/255f,230f/255f,170f/255f);
			}else{
				c[i] = new Color(237f/255f,27f/255f,36f/255f);
			}
			c[i].a = 255;
		}
	}

	void FilterOBAMA(Color[] c){
		this.GetComponent<GUITexture> ().enabled = false;
		
		float avBrightness = 0;
		for (int i=0; i<data.Length; i++) {
			avBrightness += ((data[i].r + data[i].g + data[i].b)/3);
		}
		avBrightness /= c.Length;
		float thresh = 0.3f;
		float max1 = avBrightness + (avBrightness*(thresh*2));
		float max2 = avBrightness + (avBrightness*thresh);
		float max3 = avBrightness - (avBrightness*thresh);
		
		for(int i=0; i<data.Length; i++){
			float curAv = (data[i].r + data[i].g + data[i].b)/3;
			if(curAv > max1){
				c[i] = new Color(237f/255f,27f/255f,36f/255f);
			}else if(curAv > max2){
				c[i] = new Color(252f/255f,230f/255f,170f/255f);
			}else if(curAv > max3){
				c[i] = new Color(125f/255f,164f/255f,173f/255f);
			}else{
				c[i] = new Color(3f/255f,50f/255f,76f/255f);
			}
			c[i].a = 255;
		}
	}

	void FilterBW(Color[] c){
		/*this.GetComponent<GUITexture> ().enabled = false;

		float avBrightness = 0;
		for (int i=0; i<data.Length; i++) {
			avBrightness += ((data[i].r + data[i].g + data[i].b)/3);
		}
		avBrightness /= c.Length;
		avBrightness = 130;*/
		for(int i=0; i<data.Length; i++){
			if((data[i].r + data[i].g + data[i].b)/3 > avBrightness){
				//bright
				c[i] = new Color(255,255,255);
			}else{
				//dark
				c[i] = new Color(0,0,0);
			}
			//c[i].a = 255;
		}
		
		for (int i=0; i<c.Length; i++) {
			//isRecursive = false;
			FloodFill (i);
			/*if(isRecursive){
				if(b_count < (c_w*c_h*0.002f)){
					blobs.Remove(blobs[blobs.Count-1]);
				}else{
					blobs[blobs.Count-1].GetComponent<DSAR_Blob>().blob_size = b_count;
				}
			}*/
		}
		
		/*foreach (GameObject b in blobs.ToArray()) {
			if(b.GetComponent<DSAR_Blob>().blob_size < (c_w*c_h/10)){
				blobs.Remove(b);
			}
		}*/
		
		/*foreach (GameObject b in blobs) {
			DSAR_Blob dp = b.GetComponent<DSAR_Blob>();
			float tempX, tempY;
			float posX = 0f;
			float posY = 0f;
			//Debug.Log ("Pixel Count: " + dp.pixels_in_blob.Count);
			for(int i=0; i<dp.pixels_in_blob.Count; i++){
				tempX = dp.pixels_in_blob[i]%c_w;
				tempY = Mathf.Floor(dp.pixels_in_blob[i]/c_w);
				posX += tempX;
				posY += tempY;
				if(tempX > dp.max_x)
					dp.max_x = tempX;
				if(tempX < dp.min_x)
					dp.min_x = tempX;
				if(tempY > dp.max_y)
					dp.max_y = tempY;
				if(tempY < dp.min_y)
					dp.min_y = tempY;
			}
			posX /= dp.pixels_in_blob.Count;
			posY /= dp.pixels_in_blob.Count;
			dp.blob_position_center = new Vector2((int)posX, (int)posY);
			//Debug.Log ("Blob Center: " + dp.blob_position_center);

			GUITexture blobFrame = b.GetComponent<GUITexture>();
			blobFrame.pixelInset = new Rect(dp.min_x, dp.min_y, (dp.max_x-dp.min_x), (dp.max_y-dp.min_y));
		}*/
		
		Debug.Log ("blobs: " + blobs.Count);
		blobs.Clear ();
		b_count = 0;
	}


	/**********************************************************************************/
	// http://geomalgorithms.com/a10-_hull-1.html#chainHull_2D()
	/**********************************************************************************/
	
	
	// isLeft(): tests if a point is Left|On|Right of an infinite line.
	//    Input:  three points P0, P1, and P2
	//    Return: >0 for P2 left of the line through P0 and P1
	//            =0 for P2 on the line
	//            <0 for P2 right of the line
	//    See: Algorithm 1 on Area of Triangles
	private float isLeft( Vector2 P0, Vector2 P1, Vector2 P2 )
	{
		return (P1.x - P0.x)*(P2.y - P0.y) - (P2.x - P0.x)*(P1.y - P0.y);
	}
	//===================================================================
	
	
	// chainHull_2D(): Andrew's monotone chain 2D convex hull algorithm
	//     Input:  P[] = an array of 2D points 
	//                  presorted by increasing x and y-coordinates
	//             n =  the number of points in P[]
	//     Output: H[] = an array of the convex hull vertices (max is n)
	//     Return: the number of points in H[]
	private int chainHull_2D( Vector2[] P, int n, Vector2[] H )
	{
		// the output array H[] will be used as the stack
		int    bot=0, top=(-1);   // indices for bottom and top of the stack
		int    i;                 // array scan index
		
		// Get the indices of points with min x-coord and min|max y-coord
		int minmin = 0, minmax;
		float xmin = P[0].x;
		for (i=1; i<n; i++)
			if (P[i].x != xmin) break;
		minmax = i-1;
		if (minmax == n-1) {       // degenerate case: all x-coords == xmin
			H[++top] = P[minmin];
			if (P[minmax].y != P[minmin].y) // a  nontrivial segment
				H[++top] =  P[minmax];
			H[++top] = P[minmin];            // add polygon endpoint
			return top+1;
		}
		
		// Get the indices of points with max x-coord and min|max y-coord
		int maxmin, maxmax = n-1;
		float xmax = P[n-1].x;
		for (i=n-2; i>=0; i--)
			if (P[i].x != xmax) break;
		maxmin = i+1;
		
		// Compute the lower hull on the stack H
		H[++top] = P[minmin];      // push  minmin point onto stack
		i = minmax;
		while (++i <= maxmin)
		{
			// the lower line joins P[minmin]  with P[maxmin]
			if (isLeft( P[minmin], P[maxmin], P[i])  >= 0 && i < maxmin)
				continue;           // ignore P[i] above or on the lower line
			
			while (top > 0)         // there are at least 2 points on the stack
			{
				// test if  P[i] is left of the line at the stack top
				if (isLeft(  H[top-1], H[top], P[i]) > 0)
					break;         // P[i] is a new hull  vertex
				else
					top--;         // pop top point off  stack
			}
			H[++top] = P[i];        // push P[i] onto stack
		}
		
		// Next, compute the upper hull on the stack H above  the bottom hull
		if (maxmax != maxmin)      // if  distinct xmax points
			H[++top] = P[maxmax];  // push maxmax point onto stack
		bot = top;                  // the bottom point of the upper hull stack
		i = maxmin;
		while (--i >= minmax)
		{
			// the upper line joins P[maxmax]  with P[minmax]
			if (isLeft( P[maxmax], P[minmax], P[i])  >= 0 && i > minmax)
				continue;           // ignore P[i] below or on the upper line
			
			while (top > bot)     // at least 2 points on the upper stack
			{
				// test if  P[i] is left of the line at the stack top
				if (isLeft(  H[top-1], H[top], P[i]) > 0)
					break;         // P[i] is a new hull  vertex
				else
					top--;         // pop top point off  stack
			}
			H[++top] = P[i];        // push P[i] onto stack
		}
		if (minmax != minmin)
			H[++top] = P[minmin];  // push  joining endpoint onto stack
		
		return top+1;
	}

}





