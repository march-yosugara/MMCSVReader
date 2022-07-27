using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.Text;

namespace MMCSVReader
{
    /// <summary>
    /// CSV Reader Class
    /// </summary>
    public class CSVReader : IDisposable
    {
        #region 【Sample】CSVRecordClass
        /// <summary>
        /// Sample class to store CSV data
        /// </summary>
        /// <remarks>
        /// The following is mapping by CSV index with [Index].
        /// In addition, mapping by header column value with [Name],
        /// Ignore mapping with [Ignore] can be set.
        /// </remarks>
        private class CSVRecordClass
        {
            /// <summary>
            /// Column 1(Index:0)
            /// </summary>
            [Index(0)]
            public string? Id
            {
                get;
                set;
            }
            /// <summary>
            /// Column 2(Index:1)
            /// </summary>
            [Index(1)]
            public string? Name
            {
                get;
                set;
            }
            /// <summary>
            /// This property is ignored by mapping(private set)
            /// </summary>
            public bool Enabled
            {
                get;
                private set;
            }
        }
        #endregion

        #region Constats
        /// <summary>
        /// Open failed・File not exists
        /// </summary>
        public static readonly long StatusDisabled = -1;
        /// <summary>
        /// No data record
        /// </summary>
        public static readonly long StatusHeadOrNoRec = 0;
        /// <summary>
        /// Available
        /// </summary>
        public static readonly long StatusAvailable = 1;
        #endregion

        #region Property
        /// <summary>
        /// CSV file path
        /// </summary>
        public string FilePath
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Encoding to read CSV file
        /// </summary>
        /// <remarks>default:UTF-8</remarks>
        public Encoding Encoding
        {
            get;
            set;
        } = Encoding.UTF8;

        /// <summary>
        /// Exists header record
        /// </summary>
        public bool HasHeader
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Delimiter
        /// </summary>
        public string Delimiter
        {
            get;
            set;
        } = ",";

        /// <summary>
        /// Exists my file
        /// </summary>
        public bool Exists
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return false;

                return File.Exists(FilePath);
            }
        }

        /// <summary>
        /// Is my file opened
        /// </summary>
        public bool IsOpened
        {
            get
            {
                return _csvReader is not null;
            }
        }

        /// <summary>
        /// Is my file loaded
        /// </summary>
        public bool HasRead
        {
            get
            {
                return IsModeP ? _phasRead : _hasRead;
            }
        }
        
        /// <summary>
        /// P mode(Reading with Parser)
        /// </summary>
        public bool IsModeP
        {
            get;
            private set;
        } = false;

        /// <summary>
        /// Current record count
        /// </summary>
        public long NowRowCount
        {
            get;
            private set;
        } = StatusDisabled;

        /// <summary>
        /// All record count
        /// </summary>
        /// <remarks>Don't get while reading, because using the common Reader</remarks>
        public long Count
        {
            get
            {
                long cnt = 0;

                if (!Open())
                    return StatusDisabled;
                
                while (Read())
                    cnt++;

                Close(true);

                return cnt;
            }
        }
        #endregion

        #region Member
        /// <summary>
        /// Dispose flagment
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Stream reader
        /// </summary>
        private StreamReader? _stream;

        /// <summary>
        /// CSV reader
        /// </summary>
        private CsvReader? _csvReader;

        /// <summary>
        /// Private flagment while reading for Class(R mode:Reading with Reader)
        /// </summary>
        private bool _hasRead = false;

        /// <summary>
        /// Private flagment while reading for List(P mode:Reading with Parser)
        /// </summary>
        private bool _phasRead = false;
        #endregion

        #region IDisposable
        /// <summary>
        /// Dispose(Base)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(disposing)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_csvReader is not null)
                        _csvReader.Dispose();

                    if (_stream is not null)
                        _stream.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public CSVReader()
        {
            // For custom use
        }

        /// <summary>
        /// Constructor(Until open with configuration)
        /// </summary>
        /// <param name="prmCsvFile">CSV file path</param>
        /// <param name="config">CSVHelper Configuration</param>
        public CSVReader(string prmCsvFile, CsvHelper.Configuration.CsvConfiguration config)
        {
            FilePath = prmCsvFile;

            Open(config);
        }

        /// <summary>
        /// Constructor(Until open)
        /// </summary>
        /// <param name="prmCsvFile">CSV file path</param>
        /// <param name="opening">Open or not</param>
        public CSVReader(string prmCsvFile, bool opening = true)
        {
            FilePath = prmCsvFile;

            if (opening)
                Open();
        }

        /// <summary>
        /// Constructor(Until open)
        /// </summary>
        /// <param name="prmCsvFile">CSV file path</param>
        /// <param name="prmEncoding">File encoding</param>
        /// <param name="opening">Open or not</param>
        public CSVReader(string prmCsvFile, Encoding prmEncoding, bool opening = true) : this(prmCsvFile, false)
        {
            Encoding = prmEncoding;

            if (opening)
                Open();
        }

        /// <summary>
        /// Constructor(Until open)
        /// </summary>
        /// <param name="prmCsvFile">CSV file path</param>
        /// <param name="prmHasHeader">Has header or not</param>
        /// <param name="prmEncoding">File encoding</param>
        /// <param name="opening">Open or not</param>
        public CSVReader(string prmCsvFile, bool prmHasHeader, Encoding prmEncoding, bool opening = true) : this(prmCsvFile, prmEncoding, false)
        {
            HasHeader = prmHasHeader;

            if (opening)
                Open();
        }

        /// <summary>
        /// Constructor(Until open)
        /// </summary>
        /// <param name="prmCsvFile">CSV file path</param>
        /// <param name="prmHasHeader">Has header or not</param>
        /// <param name="prmEncoding">File encoding</param>
        /// <param name="prmDelimiter">Delimiter</param>
        /// <param name="opening">Open or not</param>
        public CSVReader(string prmCsvFile, string prmDelimiter, bool prmHasHeader, Encoding prmEncoding, bool opening = true) : this(prmCsvFile, prmHasHeader, prmEncoding, false)
        {
            Delimiter = prmDelimiter;

            if (opening)
                Open();
        }
        #endregion

        #region Common Method
        /// <summary>
        /// Open file
        /// </summary>
        public bool Open()
        {
            try
            {
                // Initialize
                Close();

                if (!Exists)
                {
                    NowRowCount = StatusDisabled;
                    return false;
                }

                var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = HasHeader,
                    Encoding = Encoding,
                    Delimiter = Delimiter,
                };

                _stream = new StreamReader(FilePath, Encoding);
                _csvReader = new CsvReader(_stream, config);
            
                return true;
            }
            catch
            {
                Dispose();
                NowRowCount = StatusDisabled;
                return false;
            }
        }

        /// <summary>
        /// Open file with configuration
        /// </summary>
        /// <param name="config">CSVHelperConfig</param>
        public bool Open(CsvHelper.Configuration.CsvConfiguration config)
        {
            try
            {
                Close();

                if (!Exists)
                {
                    NowRowCount = StatusDisabled;
                    return false;
                }

                _stream = new StreamReader(FilePath, Encoding);
                _csvReader = new CsvReader(_stream, config);

                Encoding = config.Encoding;
                HasHeader = config.HasHeaderRecord;
                Delimiter = config.Delimiter;

                return true;
            }
            catch
            {
                Dispose();
                NowRowCount = StatusDisabled;
                return false;
            }
        }

        /// <summary>
        /// Close file and initialize
        /// </summary>
        /// <param name="disposing">With dispose</param>
        public void Close(bool disposing = false)
        {
            _hasRead = false;
            _phasRead = false;
            IsModeP = false;
            NowRowCount = StatusDisabled;

            if (_stream is not null)
                _stream.Dispose();

            if (_csvReader is not null)
                _csvReader.Dispose();
            
            if (disposing)
                Dispose();
        }
        #endregion

        #region P Mode Method
        /// <summary>
        /// Read one record
        /// </summary>
        /// <returns>EOF</returns>
        public bool PRead()
        {
            if (_csvReader is null)
                return false;
            
            // Not P mode and Reader already read is illegal(It's reading for Class)
            if (!IsModeP && _hasRead)
                return false;

            // Has header & unread / Not has header & unread
            if (HasHeader && NowRowCount == StatusDisabled)
            {
                NowRowCount = StatusHeadOrNoRec;
                if (!_csvReader.Parser.Read())
                    return false;
            }
            else if (NowRowCount == StatusDisabled)
            {
                NowRowCount = StatusHeadOrNoRec;
            }

            var ret = _csvReader.Parser.Read();
            IsModeP = true;
            _phasRead = true;
            NowRowCount = ret ? NowRowCount + 1 : NowRowCount;

            return ret;
        }

        /// <summary>
        /// Get record data
        /// </summary>
        /// <returns>List of strings</returns>
        public List<string> Record()
        {
            if (_csvReader is null)
                return new List<string>();

            // Read when unread in P mode
            if (!_phasRead)
            {
                if (!PRead())
                    return new List<string>();
            }

            var _work = _csvReader.Parser.Record;
            if (_work is null)
                return new List<string>();
            else
                return _work.ToList();
        }

        /// <summary>
        /// Get all records data in List
        /// </summary>
        /// <remarks>Although it's said all records, it can processe one by one, because using yield.</remarks>
        /// <returns>Enumerable list</returns>
        public IEnumerable<List<string>>? AllRecords()
        {
            if (!Open())
                yield break;

            while (PRead())
                yield return Record();
        }
        #endregion

        #region R Mode Method
        /// <summary>
        /// Read one record
        /// </summary>
        /// <returns>EOF</returns>
        public bool Read()
        {
            if (_csvReader is null)
                return false;
            
            // P mode and Parser already read is illegal(It's reading for List)
            if (IsModeP && _phasRead)
                return false;

            if (HasHeader && NowRowCount == StatusDisabled)
            {
                NowRowCount = StatusHeadOrNoRec;
                if (_csvReader.Read())
                    _csvReader.ReadHeader();
                else
                    return false;
            }
            else if (NowRowCount == StatusDisabled)
            {
                NowRowCount = StatusHeadOrNoRec;
            }

            var ret = _csvReader.Read();
            _hasRead = true;
            NowRowCount = ret ? NowRowCount + 1 : NowRowCount;

            return ret;
        }

        /// <summary>
        /// Get record data
        /// </summary>
        /// <returns>Defined class</returns>
        public T? Record<T>()
        {
            if (_csvReader is null)
                return default;

            // Read when unread in R mode
            if (!_hasRead)
            {
                if (!Read())
                    return default;
            }
            
            return _csvReader.GetRecord<T>();
        }

        /// <summary>
        /// Get all records data in List
        /// </summary>
        /// <returns>Enumerable list</returns>
        public IEnumerable<T>? AllRecords<T>()
        {
            if (_csvReader is null)
                return default;

            if (!Open())
                return null;

            return _csvReader.GetRecords<T>();
        }
        #endregion
    }
}
