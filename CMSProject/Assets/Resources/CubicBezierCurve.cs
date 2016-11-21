// Tom McCauley thomas.mccauley@cern.ch

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CubicBezierCurve {
	
	private Vector3[] controlPoints;
	//private Vector3[] curvePoints;
	private List<Vector3> curvePoints;
	private int nSegments;
	
	public CubicBezierCurve(Vector3[] cps, int nsegs) {
		controlPoints = cps;
		nSegments = nsegs;
		//curvePoints = new Vector3[nSegments];
		curvePoints = new List<Vector3>();
		makeCurve();
	}

	private Vector3 makePoint(float t) {
		float u = 1 - t;
		float u2 = u * u;
		float u3 = u2 * u;
		float t2 = t * t;
		float t3 = t2 * t;

		Vector3 pt = u3 * controlPoints [0];
		pt += 3 * u2 * t * controlPoints [1];
		pt += 3 * u * t2 * controlPoints [2];
		pt += t3 * controlPoints [3];
			
		return pt;
	}

	private void makeCurve() {
		float dt = 1.0f/nSegments;
		float t;
		
		for ( int i = 1; i <= nSegments; i++ ) {
			t = i*dt;
			Vector3 pt = makePoint(t);
			//curvePoints[i-1] = pt;
			curvePoints.Add(pt);
		}
	}

	public List<Vector3> GetCurvePoints() {
		return curvePoints;
	}
}
