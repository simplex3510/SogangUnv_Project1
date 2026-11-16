namespace Game.Logistics
{
    /// <summary>일일 별점 판정(기획서 12.4). 상태를 갖지 않는 순수 함수라 단독으로 검증할 수 있다.</summary>
    public static class DailyStarRating
    {
        /// <summary>별 0~3개를 반환한다. 발생 물류가 없으면 null(별점 미표시).</summary>
        /// <param name="total">발생 물류 수 N</param>
        /// <param name="success">조건부 성공 수 S</param>
        /// <param name="finalAmount">최종 정산 금액</param>
        /// <param name="threshold2">별 2개 경계값 T2</param>
        /// <param name="threshold1">별 1개 경계값 T1</param>
        public static int? Evaluate(int total, int success, float finalAmount, float threshold2, float threshold1)
        {
            if (total <= 0)
            {
                return null;
            }

            // 정확히 0은 손실이 아니다
            if (finalAmount < 0f)
            {
                return 0;
            }

            // R = 1 판정은 부동소수 비교를 피하려고 정수로 한다
            if (success >= total)
            {
                return 3;
            }

            float ratio = (float)success / total;
            if (ratio >= threshold2)
            {
                return 2;
            }

            return ratio >= threshold1 ? 1 : 0;
        }
    }
}
