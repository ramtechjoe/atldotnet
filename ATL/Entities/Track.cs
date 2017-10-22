using ATL.AudioData;
using Commons;
using System;
using System.Collections.Generic;
using System.IO;

namespace ATL
{
	/// <summary>
	/// High-level class for audio file manipulation
	/// </summary>
	public class Track
	{
		public Track() {}

        public Track(string iPath)
        {
            Path = iPath;
            Update();
        }

		public string Path;
		public string Title;
		public string Artist;
        public string Composer;
		public string Comment;
		public string Genre;
		public string Album;
        public string OriginalAlbum;
        public string OriginalArtist;
        public string Copyright;
        public string Description;
        public string Publisher;
        public string AlbumArtist;
        public string Conductor;
        public int Year;
		public int Bitrate;
        public double SampleRate;
        public bool IsVBR;
		public int CodecFamily;
		public int Duration;		
		public int TrackNumber;
        public int DiscNumber;
        public int Rating;
        public IList<TagData.PictureInfo> PictureTokens = null;

        public IDictionary<string, string> AdditionalFields;
        private ICollection<string> initialAdditionalFields; // Initial fields, used to identify removed ones

        private IList<TagData.PictureInfo> embeddedPictures = null;
        private ICollection<TagData.PictureInfo> initialEmbeddedPictures; // Initial fields, used to identify removed ones

        private AudioFileIO fileIO;


        // ========== METHODS

        public IList<TagData.PictureInfo> EmbeddedPictures
        {
            get
            {
                return getEmbeddedPictures();
            }
        }

        private IList<TagData.PictureInfo> getEmbeddedPictures()
        {
            if (null == embeddedPictures)
            {
                embeddedPictures = new List<TagData.PictureInfo>();
                initialEmbeddedPictures = new List<TagData.PictureInfo>();

                Update(new TagData.PictureStreamHandlerDelegate(readBinaryImageData));
            }

            return embeddedPictures;
        }

        protected void readBinaryImageData(ref MemoryStream s, TagData.PIC_TYPE picType, ImageFormat imgFormat, int originalTag, object picCode, int position)
        {
            TagData.PictureInfo picInfo = new TagData.PictureInfo(imgFormat, picType, originalTag, picCode, position);
            picInfo.PictureData = s.ToArray();

            // Initial pic info, without picture data
            TagData.PictureInfo initialPicInfo = new TagData.PictureInfo(imgFormat, picType, originalTag, picCode, position);
            initialPicInfo.PictureHash = HashDepot.Fnv1a.Hash32(picInfo.PictureData);

            embeddedPictures.Add(picInfo);
            initialEmbeddedPictures.Add(initialPicInfo);
        }

        protected void Update(TagData.PictureStreamHandlerDelegate pictureStreamHandler = null)
        {
            // TODO when tag is not available, customize by naming options // tracks (...)
            fileIO = new AudioFileIO(Path, pictureStreamHandler, Settings.ReadAllMetaFrames);

            Title = fileIO.Title;
            if ("" == Title || null == Title)
            {
                Title = System.IO.Path.GetFileNameWithoutExtension(Path); // TODO - this should be an option, as returned value is not really read from the tag
            }
            Artist = Utils.ProtectValue(fileIO.Artist);
            Composer = Utils.ProtectValue(fileIO.Composer);
            Comment = Utils.ProtectValue(fileIO.Comment);
            Genre = Utils.ProtectValue(fileIO.Genre);
            OriginalArtist = Utils.ProtectValue(fileIO.OriginalArtist);
            OriginalAlbum = Utils.ProtectValue(fileIO.OriginalAlbum);
            Description = Utils.ProtectValue(fileIO.GeneralDescription);
            Copyright = Utils.ProtectValue(fileIO.Copyright);
            Publisher = Utils.ProtectValue(fileIO.Publisher);
            AlbumArtist = Utils.ProtectValue(fileIO.AlbumArtist);
            Conductor = Utils.ProtectValue(fileIO.Conductor);
            Year = fileIO.IntYear;
            Album = fileIO.Album;
            TrackNumber = fileIO.Track;
            DiscNumber = fileIO.Disc;
            Bitrate = fileIO.IntBitRate;
            CodecFamily = fileIO.CodecFamily;
            Duration = fileIO.IntDuration;
            Rating = fileIO.Rating;
            IsVBR = fileIO.IsVBR;
            SampleRate = fileIO.SampleRate;

            AdditionalFields = fileIO.AdditionalFields;
            initialAdditionalFields = fileIO.AdditionalFields.Keys;

            PictureTokens = new List<TagData.PictureInfo>(fileIO.PictureTokens);

            if (null == pictureStreamHandler && embeddedPictures != null)
            {
                embeddedPictures.Clear();
                initialEmbeddedPictures.Clear();
                embeddedPictures = null;
                initialEmbeddedPictures = null;
            }
        }

        private TagData toTagData()
        {
            TagData result = new TagData();

            result.Title = Title;
            result.Artist = Artist;
            result.Composer = Composer;
            result.Comment = Comment;
            result.Genre = Genre;
            result.OriginalArtist = OriginalArtist;
            result.OriginalAlbum = OriginalAlbum;
            result.GeneralDescription = Description;
            result.Copyright = Copyright;
            result.Publisher = Publisher;
            result.AlbumArtist = AlbumArtist;
            result.Conductor = Conductor;
            result.RecordingYear = Year.ToString();
            result.Album = Album;
            result.TrackNumber = TrackNumber.ToString();
            result.DiscNumber = DiscNumber.ToString();
            result.Rating = Rating.ToString();

            foreach (string s in AdditionalFields.Keys)
            {
                result.AdditionalFields.Add(new TagData.MetaFieldInfo(MetaDataIOFactory.TAG_ANY, s, AdditionalFields[s]));
            }

            // Detect and tag deleted Additional fields (=those which were in initialAdditionalFields and do not appear in AdditionalFields anymore)
            foreach (string s in initialAdditionalFields)
            {
                if (!AdditionalFields.ContainsKey(s))
                {
                    TagData.MetaFieldInfo metaFieldToDelete = new TagData.MetaFieldInfo(MetaDataIOFactory.TAG_ANY, s, "");
                    metaFieldToDelete.MarkedForDeletion = true;
                    result.AdditionalFields.Add(metaFieldToDelete);
                }
            }

            result.Pictures = new List<TagData.PictureInfo>();
            if (embeddedPictures != null) foreach (TagData.PictureInfo targetPic in embeddedPictures) targetPic.Flag = 0;

            if (initialEmbeddedPictures != null)
            {
                foreach (TagData.PictureInfo picInfo in initialEmbeddedPictures)
                {
                    // Detect and tag deleted pictures (=those which were in initialEmbeddedPictures and do not appear in embeddedPictures anymore)
                    if (!embeddedPictures.Contains(picInfo))
                    {
                        TagData.PictureInfo picToDelete = new TagData.PictureInfo(picInfo);
                        picToDelete.MarkedForDeletion = true;
                        result.Pictures.Add(picToDelete);
                    }
                    else // Only add new additions (pictures identical to initial list will be kept, and do not have to make it to the list, or else a duplicate will be created)
                    {
                        foreach (TagData.PictureInfo targetPic in embeddedPictures)
                        {
                            if (targetPic.Equals(picInfo))
                            {
                                // Compare picture contents
                                uint newPictureHash = HashDepot.Fnv1a.Hash32(targetPic.PictureData);

                                if (newPictureHash != picInfo.PictureHash)
                                {
                                    // A new picture content has been defined for an existing location
                                    result.Pictures.Add(targetPic);

                                    TagData.PictureInfo picToDelete = new TagData.PictureInfo(picInfo);
                                    picToDelete.MarkedForDeletion = true;
                                    result.Pictures.Add(picToDelete);
                                }

                                targetPic.Flag = 1;
                            }
                        }
                    }
                }

                if (embeddedPictures != null)
                {
                    foreach (TagData.PictureInfo targetPic in embeddedPictures)
                    {
                        if (0 == targetPic.Flag) // Entirely new pictures without equivalent in initialEmbeddedPictures
                        {
                            result.Pictures.Add(targetPic);
                        }
                    }
                }
            }

            return result;
        }

        public void Save()
        {
            fileIO.Save(toTagData());
            Update();
        }

        public void Remove(int tagType = MetaDataIOFactory.TAG_ANY)
        {
            fileIO.Remove(tagType);
            Update();
        }
    }
}
