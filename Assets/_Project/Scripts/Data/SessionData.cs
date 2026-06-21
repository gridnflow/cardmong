namespace Cardmong.Data
{
    /// <summary>
    /// 로그인을 제거한 뒤 게임 실행에 필요한 최소 세션 정보만 보관한다.
    /// (닉네임 표시 · 선택한 덱 ID)
    /// </summary>
    public class SessionData
    {
        public static SessionData Instance { get; } = new SessionData();

        // 기본 폰트(LiberationSans SDF)에 한글 글리프가 없어 ASCII 기본값 사용
        public string Nickname       { get; set; } = "Player";
        public int    SelectedDeckId { get; set; } = 0;

        private SessionData() { }
    }
}
