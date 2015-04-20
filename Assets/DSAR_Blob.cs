using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DSAR_Blob : MonoBehaviour {

	public int blobID;
	public int blob_size=0;
	public List<Vector2> pixels_in_blob = new List<Vector2>();
	public List<Vector2> pixels_in_bb = new List<Vector2>();
	public List<Vector2> corner_pixels = new List<Vector2> ();
	public List<GameObject> corner_gos = new List<GameObject> ();
	public int numPixelsBB = 0;
	public Vector3 blob_velocity;
	public Vector2 blob_position_center;
	public float max_x = 0;
	public float max_y = 0;
	public float min_x = Screen.width * Screen.height + 1;
	public float min_y = Screen.width * Screen.height + 1;
	public int blob_height;
	public int blob_width;
	public Vector2 topRight, topLeft, bottomRight, bottomLeft, rightTop, rightBottom, leftTop, leftBottom;
}
