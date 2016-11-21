// Tom McCauley thomas.mccauley@cern.ch

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

class CaloHit {

	public string typeName { get; private set; }

	public float energy { get; private set; }
	public Vector3[] corners { get; private set; }

	public CaloHit(string t, float e, Vector3[] c) {
		typeName = t;
		energy= e;
		corners = c;
	}
}

class Jet {
	public float et { get; private set; }
	public float eta { get; private set; }
	public float phi { get; private set; }
	public float theta { get; private set; }

	public Jet(float et, float eta, float phi, float theta ) {
		this.et = et;
		this.eta = eta;
		this.phi = phi;
		this.theta = theta;
	}
}

public class Track {

	protected float pt { get; set; }
	protected float eta { get; set; }
	protected float phi { get; set; }
	protected int charge { get; set; }

	protected List<Vector3> points { get; set; }

	public Track() {}

	public Track(float pt, float eta, float phi, int charge, List<Vector3> points) {
		this.pt = pt;
		this.eta = eta;
		this.phi = phi;
		this.charge = charge;
		this.points = points;
	}
}
	
class Muon : Track {

	public Muon() {}

	public Muon(float pt, float eta, float phi, int charge, List<Vector3> points) {
		this.pt = pt;
		this.eta = eta;
		this.phi = phi;
		this.charge = charge;
		this.points = points;
	}

}

class Electron : Track {

	public Electron() {}

	public Electron(float pt, float eta, float phi, int charge, List<Vector3> points) {
		this.pt = pt;
		this.eta = eta;
		this.phi = phi;
		this.charge = charge;
		this.points = points;
	}
}
	
class CMSEvent {

	// Keep Event information, Tracks, ECAL and HCAL hits, and muons for now
	public string runNumber { get; private set; }
	public string eventNumber { get; private set; }
	public string lumiSection { get; private set; }
	public string eventTime { get; private set; }

	public Dictionary<string, List<CaloHit>> caloHits { get; private set; }

	public List<Track> tracks { get; private set; }
	public List<Muon> muons { get; private set; }
	public List<Electron> electrons { get; private set; }
	public List<Jet> jets { get; private set; }

	private void makeCaloHits(string cname, JSONArray hits, float minEnergy) {

		List<CaloHit> chits = new List<CaloHit>();

		for (int i = 0; i < hits.Count; i++) {

			float e = hits [i] [0].AsFloat;

			if ( e < minEnergy ) 
				continue;

			Vector3[] corners = new Vector3[8];

			JSONArray f1 = hits [i] [5].AsArray;
			JSONArray f2 = hits [i] [6].AsArray;
			JSONArray f3 = hits [i] [7].AsArray;
			JSONArray f4 = hits [i] [8].AsArray;
			JSONArray b1 = hits [i] [9].AsArray;
			JSONArray b2 = hits [i] [10].AsArray;
			JSONArray b3 = hits [i] [11].AsArray;
			JSONArray b4 = hits [i] [12].AsArray;

			corners[0] = new Vector3 (f1 [0].AsFloat, f1 [1].AsFloat, f1 [2].AsFloat);
			corners[1] = new Vector3 (f2 [0].AsFloat, f2 [1].AsFloat, f2 [2].AsFloat);
			corners[2] = new Vector3 (f3 [0].AsFloat, f3 [1].AsFloat, f3 [2].AsFloat);
			corners[3] = new Vector3 (f4 [0].AsFloat, f4 [1].AsFloat, f4 [2].AsFloat);
			corners[4] = new Vector3 (b1 [0].AsFloat, b1 [1].AsFloat, b1 [2].AsFloat);
			corners[5] = new Vector3 (b2 [0].AsFloat, b2 [1].AsFloat, b2 [2].AsFloat);
			corners[6] = new Vector3 (b3 [0].AsFloat, b3 [1].AsFloat, b3 [2].AsFloat);
			corners[7] = new Vector3 (b4 [0].AsFloat, b4 [1].AsFloat, b4 [2].AsFloat);

			chits.Add(new CaloHit (cname, e, corners));
		}

		caloHits.Add (cname, chits);
	}

	private void makeMuons(JSONArray mus, JSONArray extras, JSONArray assocs) {

		for (int i = 0; i < mus.Count; i++) {

			float pt = mus [i] [0].AsFloat;
			float eta = mus [i] [4].AsFloat;
			float phi = mus [i] [3].AsFloat;
			int charge = mus [i] [1].AsInt;

			List<Vector3> pts = new List<Vector3>();

			for (int j = 0; j < assocs.Count; j++ ) {

				JSONArray assoc = assocs[j].AsArray;

				int mi = assoc[0][1].AsInt;
				int ei = assoc[1][1].AsInt;

				if ( mi == i ) { // Then we have the muon we want and now get the point data
					
					var ex = extras [ei] [0].AsArray;
					pts.Add(new Vector3 (ex [0].AsFloat, ex [1].AsFloat, ex [2].AsFloat));
				}
			}
		
			muons.Add(new Muon(pt,eta,phi,charge,pts));
		}
	}
		
	private void makeTracks(JSONArray ts, JSONArray extras, JSONArray assocs) {

		for (int i = 0; i < assocs.Count; i++) {

			// What is going on here?
			// assocs[i][][0] is the position of the objects in the file
			// but since we got them by name all we need are the indices of the
			// track and its associated innermost and outermost states (in the extra),
			// which we get below. It still confuses ME sometimes...

			JSONArray assoc = assocs[i].AsArray;

			int ti = assoc[0][1].AsInt;

			float pt  = ts[ti][2].AsFloat;

			if ( pt < 1.0 ) {
				continue;
			}

			float eta = ts [ti] [4].AsFloat;
			float phi = ts [ti] [3].AsFloat;
			int charge = ts [ti] [5].AsInt;

			int ei = assoc[1][1].AsInt;

			// What's all this then?
			// Well, we know the beginning and end points of the track as well
			// as the directions at each of those points. This in-principle gives
			// us the 4 control points needed for a cubic bezier spline.
			// The control points from the directions are determined by moving along 0.25
			// of the distance between the beginning and end points of the track.
			// This 0.25 is nothing more than a fudge factor that reproduces closely-enough
			// the NURBS-based drawing of tracks done in the old OpenGL iSpy. 
			// At some point it may be nice to implement the NURBS-based drawing...

			JSONArray ex0 = extras [ei] [0].AsArray;
			JSONArray ex1 = extras [ei] [1].AsArray;
			JSONArray ex2 = extras [ei] [2].AsArray;
			JSONArray ex3 = extras [ei] [3].AsArray;

			Vector3 ipos = new Vector3 (ex0 [0].AsFloat, ex0 [1].AsFloat, ex0 [2].AsFloat);
			Vector3 idir = new Vector3 (ex1 [0].AsFloat, ex1 [1].AsFloat, ex1 [2].AsFloat);
			idir.Normalize ();

			Vector3 opos = new Vector3 (ex2 [0].AsFloat, ex2 [1].AsFloat, ex2 [2].AsFloat);
			Vector3 odir = new Vector3 (ex3 [0].AsFloat, ex3 [1].AsFloat, ex3 [2].AsFloat);
			odir.Normalize ();

			float scale = Vector3.Distance(opos, ipos);
			scale *= 0.25f;

			Vector3[] cps = new Vector3[4] {ipos, (ipos + idir*scale), (opos - odir*scale), opos};

			CubicBezierCurve curve = new CubicBezierCurve(cps, 16);

			Debug.Assert (curve.GetCurvePoints ().Count == 16);

			Track trk = new Track (pt, eta, phi, charge, curve.GetCurvePoints ());
			tracks.Add (trk);
		}
	}

	private void makeElectrons(JSONArray ts, JSONArray extras, JSONArray assocs) {

		// This is essentially the same as for tracks but the schema is slightly different.
		// Should use Types to avoid this.

		for (int i = 0; i < assocs.Count; i++) {

			JSONArray assoc = assocs [i].AsArray;

			int ti = assoc [0] [1].AsInt;

			float pt = ts [ti] [0].AsFloat;

			if (pt < 1.0) {
				continue;
			}

			float eta = ts [ti] [1].AsFloat;
			float phi = ts [ti] [2].AsFloat;
			int charge = ts [ti] [3].AsInt;

			int ei = assoc [1] [1].AsInt;

			JSONArray ex0 = extras [ei] [0].AsArray;
			JSONArray ex1 = extras [ei] [1].AsArray;
			JSONArray ex2 = extras [ei] [2].AsArray;
			JSONArray ex3 = extras [ei] [3].AsArray;

			Vector3 ipos = new Vector3 (ex0 [0].AsFloat, ex0 [1].AsFloat, ex0 [2].AsFloat);
			Vector3 idir = new Vector3 (ex1 [0].AsFloat, ex1 [1].AsFloat, ex1 [2].AsFloat);
			idir.Normalize ();

			Vector3 opos = new Vector3 (ex2 [0].AsFloat, ex2 [1].AsFloat, ex2 [2].AsFloat);
			Vector3 odir = new Vector3 (ex3 [0].AsFloat, ex3 [1].AsFloat, ex3 [2].AsFloat);
			odir.Normalize ();

			float scale = Vector3.Distance (opos, ipos);
			scale *= 0.25f;

			Vector3[] cps = new Vector3[4] { ipos, (ipos + idir * scale), (opos - odir * scale), opos };

			CubicBezierCurve curve = new CubicBezierCurve (cps, 16);

			Debug.Assert (curve.GetCurvePoints ().Count == 16);

			Electron trk = new Electron (pt, eta, phi, charge, curve.GetCurvePoints ());
			electrons.Add (trk);
		}
	}

	private void makeJets (JSONArray js, float minEt) {

		for (int i = 0; i < js.Count; i++) {

			JSONArray j = js[i].AsArray;

			float et = j[0].AsFloat;

			if ( et < minEt ) {
				continue;
			}
				
			float eta = j[1].AsFloat;
			float theta = j[2].AsFloat;
			float phi = j[3].AsFloat;

			Jet jet = new Jet (et, eta, phi, theta);
			jets.Add (jet);
		}
	}
		
	public CMSEvent(string json) {
		
		var evt = JSON.Parse (json);

		var evt_info = evt ["Collections"] ["Event_V2"][0];

		runNumber = evt_info [0];
		eventNumber = evt_info [1];
		lumiSection = evt_info [2];
		eventTime = evt_info [5];

		var ebs = evt ["Collections"] ["EBRecHits_V2"].AsArray;
		var ees = evt ["Collections"] ["EERecHits_V2"].AsArray;
		var hbs = evt ["Collections"] ["HBRecHits_V2"].AsArray;
		var hes = evt ["Collections"] ["HERecHits_V2"].AsArray;

		caloHits = new Dictionary<string, List<CaloHit>> ();
		makeCaloHits ("EB", ebs, 0.5f);
		makeCaloHits ("EE", ees, 0.5f);
		makeCaloHits ("HB", hbs, 0.5f);
		makeCaloHits ("HE", hes, 0.5f);

		var mus = evt ["Collections"] ["GlobalMuons_V1"].AsArray;
		var extras = evt ["Collections"] ["Points_V1"].AsArray;
		var assocs = evt ["Associations"] ["MuonGlobalPoints_V1"].AsArray;

		muons = new List<Muon>();
		makeMuons (mus, extras, assocs);

		var ts = evt ["Collections"] ["Tracks_V2"].AsArray;
		extras = evt ["Collections"] ["Extras_V1"].AsArray;
		assocs = evt ["Associations"] ["TrackExtras_V1"].AsArray;

		tracks = new List<Track> ();
		makeTracks (ts, extras, assocs);

		ts = evt ["Collections"] ["GsfElectrons_V1"].AsArray;
		assocs = evt["Associations"]["GsfElectronExtras_V1"].AsArray;

		electrons = new List<Electron> ();
		makeElectrons (ts, extras, assocs);

		jets = new List<Jet> ();
		makeJets(evt["Collections"]["Jets_V1"].AsArray, 20.0f);
	}
}
