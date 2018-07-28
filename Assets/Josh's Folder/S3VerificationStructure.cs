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

namespace S3Verification
{
	public class S3VerificationStructure : MonoBehaviour
	{
		public Dictionary<string, FileEntry> FileList;
		public delegate void OnAsyncRetrievedEvent(Dictionary<string, FileEntry> fileEntryDictionary);
		public delegate void onCompleteS3DownloadObject();
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
			FileList = new Dictionary<string, FileEntry> ();
			UnityInitializer.AttachToGameObject(this.gameObject);
			AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;

			GetLocalFiles ();

			S3PostFiles ();

			S3ListObjects ();
			OnAsyncRetrieved += new OnAsyncRetrievedEvent (OnAsyncRetrievedTest);

		}

		void Update()
		{
			if (Input.GetKey("down"))
			{
				GetObject ("HG.txt", "C:\\Users\\Joshu\\Desktop\\Test\\HG.txt");
				//GetObject("WHY.PNG", "C:\\Users\\Joshu\\Desktop\\Test\\WHY.PNG"); 
				//GetObject("test.unitypackage", "C:\\Users\\Joshu\\Desktop\\Test\\test.unitypackage");
			}
		}

		void OnAsyncRetrievedTest(Dictionary<string, FileEntry> fileEntryDictionary) {
			FileList = fileEntryDictionary;
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
				return _s3Client;
			}
		}
		#endregion

		#region GetLocalFiles
		public Dictionary<string, FileEntry> GetLocalFiles()
		{
			Dictionary<string, FileEntry> localFileList = new Dictionary<string, FileEntry> ();

			foreach (string path in GetAllFilePaths()) {
				localFileList.Add(path, new FileEntry(Path.GetFileName(path), path, new FileInfo(path).Length, FileEntry.Status.Unmodified, File.GetLastWriteTime(path), File.GetCreationTime(path)));
			}

			return localFileList;
		}

		#region S3 ListObjects()
		public void S3ListObjects()
		{
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
								FileEntry entry = new FileEntry(o.Key, GetURL(o.Key), o.Size, FileEntry.Status.Unmodified, (DateTime)o.LastModified, (DateTime)o.LastModified);
								FileList.Add(entry.path, entry);
							});

						if (OnAsyncRetrieved != null) {
							OnAsyncRetrieved(FileList);
						}
					} 

					catch (AmazonS3Exception e) 
					{
						throw e;
					}
				});
		}
		#endregion

		#region S3 GetObjects
		public void S3GetObjects()
		{
			foreach (KeyValuePair<string, FileEntry> temp in FileList) 
			{

			}
		}
		#endregion

		#region S3 PostFiles
		public void S3PostFiles()
		{
			foreach (string path in GetAllFilePaths()) 
			{
				PostObject (path, Path.GetFileName(path));
			}
		}
		#endregion

		#region Helper Functions
		public string GetURL(string k)
		{
			return "https://s3.amazonaws.com/" + S3BucketName + "/" + k;
		}
			
		public string[] GetAllFilePaths()
		{
			return Directory.GetFiles (Application.persistentDataPath, "*.*", SearchOption.AllDirectories);
		}

		public void GetObject(string fileName, string key)
		{
			Debug.Log ("Inside GetObject");
			Client.GetObjectAsync (S3BucketName, fileName, (responseObj) => 
			{
					MemoryStream memStream = new MemoryStream();
					var response = responseObj.Response;
					BufferedStream buffStream = new BufferedStream(response.ResponseStream);
					byte[] buffer = new byte[0x2000];
					int count = 0;

					while ((count = buffStream.Read(buffer, 0, buffer.Length)) > 0)
					{
						
						memStream.Write(buffer, 0, count);
					}

					string tempFile = Path.GetTempFileName();
					FileStream newFile = new FileStream(tempFile, FileMode.Create);

					memStream.Position = 0;
					memStream.CopyTo(newFile);
					newFile.Close();

					if (File.Exists(key))
						File.Delete(key);
					File.Move(tempFile, key);
			});
			Debug.Log ("Finished with Upload");
		}

		public void PostObject(string path, string key)
		{
			var stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var request = new PostObjectRequest () 
			{
				Bucket = S3BucketName,
				Key = key,
				InputStream = stream,
				CannedACL = S3CannedACL.PublicRead
			};

			Client.PostObjectAsync (request, (requestObject) => 
			{
				if (requestObject.Exception != null)
				{
					throw requestObject.Exception;
				}
			});
		}
			
		#endregion
	} // End Class
} // End Namespace
#endregion