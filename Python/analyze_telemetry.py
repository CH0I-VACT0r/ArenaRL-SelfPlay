import pandas as pd
import numpy as np
import os
import streamlit as st

def analyze_arena_telemetry(file_path):
    # print 대신 st.error, st.warning을 사용하여 웹 브라우저에 알림을 띄웁니다.
    if not os.path.exists(file_path):
        st.error(f"[오류] '{file_path}' 파일이 존재하지 않습니다. 유니티를 먼저 구동하여 데이터를 쌓으십시오.")
        return

    # 1. CSV 데이터 로드
    df = pd.read_csv(file_path)
    
    if df.empty:
        st.warning("[경고] CSV 파일이 비어 있습니다.")
        return

    # 2. 개별 스킬 지표 (Skill Metrics) 계산
    df['Hit_Rate'] = np.where(df['CastCount'] > 0, df['HitCount'] / df['CastCount'], 0.0)
    df['Miss_Rate'] = np.where(df['CastCount'] > 0, (df['CastCount'] - df['HitCount']) / df['CastCount'], 0.0)
    df['Charge_Success_Rate'] = np.where(df['ChargeAttempt'] > 0, df['ChargeSuccess'] / df['ChargeAttempt'], 0.0)
    df['Cc_Success_Rate'] = np.where(df['CcAttempt'] > 0, df['CcSuccess'] / df['CcAttempt'], 0.0)

    # 3. 에이전트 종합 전투 지표 (Combat Metrics) 계산
    agent_summary = df.groupby(['Episode', 'ClassId']).agg({
        'SurvivalTime': 'first',      
        'DamageTaken': 'first',       
        'AvgDistance': 'first',       
        'IsWin': 'first',             
        'TotalDamage': 'sum',         
        'CastCount': 'sum'            
    }).reset_index()

    # 4. 클래스별(전사 vs 마법사) 최종 평균 통계 도출
    final_report = agent_summary.groupby('ClassId').agg({
        'IsWin': 'mean',               
        'TotalDamage': 'mean',         
        'DamageTaken': 'mean',          
        'SurvivalTime': 'mean',        
        'AvgDistance': 'mean'          
    }).rename(columns={'IsWin': 'Win_Rate'})

    total_dmg_sum = agent_summary.groupby('ClassId')['TotalDamage'].sum()
    total_time_sum = agent_summary.groupby('ClassId')['SurvivalTime'].sum()
    final_report['DPS'] = np.where(total_time_sum > 0, total_dmg_sum / total_time_sum, 0.0)

    # 컬럼 순서 재배치
    final_report = final_report[['Win_Rate', 'TotalDamage', 'DamageTaken', 'DPS', 'SurvivalTime', 'AvgDistance']]

    # 클래스 이름 매핑 (0: 전사, 1: 마법사)
    class_names = {0: "전사 (Warrior)", 1: "마법사 (Mage)"}
    final_report.index = final_report.index.map(class_names)

    # ==========================================
    # [복구됨] 스킬별 세부 지표 계산 로직 
    # ==========================================
    skill_report = df.groupby(['ClassId', 'SkillId']).agg({
        'CastCount': 'mean',
        'Hit_Rate': 'mean',
        'Cc_Success_Rate': 'mean',
        'Charge_Success_Rate': 'mean',
        'TotalDamage': 'mean'
    }).rename(columns={
        'CastCount': 'Avg_Cast_Count',
        'Hit_Rate': 'Avg_Hit_Rate',
        'TotalDamage': 'Avg_Skill_Damage'
    })
    
    # 더미 데이터(-1) 제외
    skill_report = skill_report.query("SkillId >= 0")
    # ==========================================

    # Streamlit 웹 대시보드 출력
    st.title("ARENA AI 전투 밸런스 분석 리포트")
    
    st.subheader("[1] 클래스별 종합 전투 지표")
    st.dataframe(final_report.round(2), use_container_width=True)

    st.subheader("[2] 스킬별 세부 지표")
    st.dataframe(skill_report.round(2), use_container_width=True)

if __name__ == "__main__":
    # 현재 파이썬 스크립트가 위치한 폴더의 절대 경로를 동적으로 추출
    current_dir = os.path.dirname(os.path.abspath(__file__))
    UNITY_ASSETS_PATH = os.path.join(current_dir, "..", "My project", "Assets", "Arena_Telemetry.csv")
    
    # 경로에 파일이 없다면, 파이썬 스크립트와 동일한 폴더에서 찾도록
    if not os.path.exists(UNITY_ASSETS_PATH):
        UNITY_ASSETS_PATH = os.path.join(current_dir, "Arena_Telemetry.csv")
        
    analyze_arena_telemetry(os.path.normpath(UNITY_ASSETS_PATH))