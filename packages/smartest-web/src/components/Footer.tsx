interface FooterProps {
    showLinks?: boolean
}

export default function Footer({ showLinks = false }: FooterProps) {
    return (
        <footer style={{
            background: '#fff',
            borderTop: '1px solid #e2e8f4',
            padding: '15px 45px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexShrink: 0,
            flexWrap: 'wrap',
            gap: 8,
        }}>
            {/* Brand */}
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{
                    fontFamily: "'DM Serif Display', serif",
                    fontSize: 14, fontWeight: 700, color: '#0f1e3d',
                }}>
                    Smar<span style={{ color: '#4f8ef7' }}>Test</span>
                </span>
            </div>



            {/* Copyright */}
            <span style={{ fontSize: 11, color: '#aab4cc' }}>
                SmarTest — ENSA Oujda · Génie Informatique · 2025–2026
            </span>
        </footer>
    )
}
