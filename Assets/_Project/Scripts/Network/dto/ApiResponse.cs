namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class ApiResponse<T>
    {
        public bool    Success { get; set; }
        public T       Data    { get; set; }
        public ApiError Error  { get; set; }
    }

    [System.Serializable]
    public class ApiError
    {
        public string Code    { get; set; }
        public string Message { get; set; }
    }
}
