using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.Runtime;
using Amazon.CognitoIdentity;

/*
 *  Objective: S3 interface with Unity to allow for:
 * 	(1) Upload a local Directory onto S3
 *  (2) Read all files from S3 into a dictionary<string, FileEntry>
 *  (3) Download actual files from S3 to Local (replacing flagged files)
*/

/* NOTES:
 * (1) Assumption is taken for now, all files from S3 to Local are treated as Unmodified or Master Copies
 * (2) 
*/

namespace S3Verification
{
	public class S3VerificationStructure : MonoBehaviour
	{
		public Dictionary<string, FileEntry> FileList;
		public delegate void OnAsyncRetrievedEvent(Dictionary<string, FileEntry> fileEntryDictionary);
		public OnAsyncRetrievedEvent OnAsyncRetrieved;

		#region S3 Initilization
		public string S3BucketName = null;
		public string IdentityPoolId = "";
		public string CognitoIdentityRegion = RegionEndpoint.USEast1.SystemName;
		private RegionEndpoint _CognitoIdentityRegion
		{
			get { return RegionEndpoint.GetBySystemName(CognitoIdentityRegion); }
		}
		public string S3Region = RegionEndpoint.USEast1.SystemName;
		private RegionEndpoint _S3Region
		{
			get { return RegionEndpoint.GetBySystemName(S3Region); }
		}
		#endregion

		void Start()
		{
			UnityInitializer.AttachToGameObject(this.gameObject);
			AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;

			S3GetObjects ("Bloopers.");
			OnAsyncRetrieved += new OnAsyncRetrievedEvent (OnAsyncRetrievedTest);
		}

		void OnAsyncRetrievedTest(Dictionary<string, FileEntry> fileEntryDictionary) {
			Debug.Log ("[This is the event response through delegates]fileList Count [Inside responseObject]: " + fileEntryDictionary.Count);
		}

		#region private members
		private IAmazonS3 _s3Client;
		private AWSCredentials _credentials;

		private AWSCredentials Credentials
		{
			get
			{
				if (_credentials == null)
					_credentials = new CognitoAWSCredentials(IdentityPoolId, _CognitoIdentityRegion);
				return _credentials;
			}
		}

		private IAmazonS3 Client
		{
			get
			{
				if (_s3Client == null)
				{
					_s3Client = new AmazonS3Client(Credentials, _S3Region);
				}
				//test comment
				return _s3Client;
			}
		}
		#endregion

		#region GetLocalFiles <DEBUG> [Done - Needs Testing]
		// Populate a Dictionary<string, FileEntry> with all files from a local directory
		// For testing purposes, the FileList in this struct will be used for both uploading a directory and recieving a directory.
		public Dictionary<string, FileEntry> GetLocalFiles()
		{
			Dictionary<string, FileEntry> localFileList = new Dictionary<string, FileEntry> ();

			foreach (string path in GetAllFilePaths()) {
				localFileList.Add(path, new FileEntry(Path.GetFileName(path), path, new FileInfo(path).Length, FileEntry.Status.Unmodified, File.GetLastWriteTime(path), File.GetCreationTime(path)));
			}

			return localFileList;
		}

		#region S3 GetObjects [Not - Done]
		// Return a Dictionary of all objects in terms of files
		// NOTE: ListObjects method can only return only 1000 objects
		// 		 Anything more, we'll have to use markers to advance to the next set of 1000 objects.
		public void S3GetObjects(string bucket)
		{
			Dictionary<string, FileEntry> fileList = new Dictionary<string, FileEntry> ();

			var request = new ListObjectsRequest () 
			{
				BucketName = S3BucketName
			};
					
			Client.ListObjectsAsync (request, (responseObject) => 
			{
				try 
				{
					responseObject.Response.S3Objects.ForEach ((o) => 
					{
						FileEntry entry = new FileEntry(o.Key, o.Key, o.Size, FileEntry.Status.Unmodified, (DateTime)o.LastModified, (DateTime)o.LastModified);
						Debug.Log ("Found object with key: " + o.Key + " size: " + o.Size + " last modification date: " + (DateTime)o.LastModified);
						fileList.Add(entry.path, entry);
						Debug.Log ("fileList Count [Inside responseObject]: " + fileList.Count);
					});

						if (OnAsyncRetrieved != null) {
							OnAsyncRetrieved(fileList);
						}
				} 

				catch (AmazonS3Exception e) 
				{
					throw e;
				}
			});

			Debug.Log ("[This is on the ASYNC function] fileList Count [Inside Function]: " + fileList.Count);

		}
		#endregion

		#region S3 PostFiles <DEBUG> [Not - Done]
		// Posts all "objects" or files in fileList onto S3 with valid bucket name
		public void S3PostFiles()
		{
			foreach (string path in GetAllFilePaths()) 
			{
				PostObject (path, Path.GetFileName(path));
			}
		}
		#endregion

		#region Helper Functions
		// Returns an array of strings of all paths in a specific directory
		public string[] GetAllFilePaths()
		{
			return Directory.GetFiles ("C:\\Users\\Joshu\\Desktop\\Test", "*.*", SearchOption.AllDirectories);
		}

		// Handles a single Posting of an Object to an S3 Bucket
		public void PostObject(string path, string key)
		{
			var stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var request = new PostObjectRequest () 
			{
				Bucket = S3BucketName,
				Key = key,
				InputStream = stream,
				CannedACL = S3CannedACL.Private
			};

			Client.PostObjectAsync (request, (requestObject) => 
			{
					if (requestObject.Exception == null)
					{
						Debug.Log("Successful Post");
					}
					else
					{
						Debug.Log("\nException while posting the result object!");	
					}
			});
		}
		#endregion
	} // End Class
} // End Namespace
#endregion