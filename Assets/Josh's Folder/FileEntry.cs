using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace S3Verification
{	
	public struct FileEntry
	{
		public enum Status {Unmodified, Added, Modified, Removed}

		public string fileName;
		public string path;
		public long fileSize;
		public Status state;
		public DateTime dTime;
		public DateTime cTime;

		public FileEntry(string n = null, string p = null, long fs = -1, Status s = Status.Unmodified, DateTime dt = default(DateTime), DateTime ct = default(DateTime))
		{
			fileName = n;
			path = p;
			fileSize = fs;
			state = s;
			dTime = dt;
			cTime = ct;
		} // End Constructor

		public static bool operator == (FileEntry fe1, FileEntry fe2)
		{
			return fe1.Equals (fe2);
		}

		public static bool operator != (FileEntry fe1, FileEntry fe2)
		{
			return !fe1.Equals(fe2);
		}

		public override bool Equals(System.Object obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;

			FileEntry fe = (FileEntry)obj;

			return fe.fileName == fileName && fe.path == path && fe.fileSize == fileSize && fe.state == state && fe.dTime == dTime;
		}

		public override int GetHashCode()
		{
			int hash = 19;
			hash = hash * 23 + fileName.GetHashCode ();
			hash = hash * 23 + path.GetHashCode ();
			return hash;
		}

	} // End Struct
} // End Namespace