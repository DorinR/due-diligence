import { Navbar } from './Navbar';

type LayoutProps = {
    children: React.ReactNode;
};

export const Layout = ({ children }: LayoutProps) => {
    return (
        <div className="flex h-screen flex-col bg-gray-100">
            <Navbar />
            <div className="flex flex-1 overflow-hidden">
                <main className="flex-1 overflow-hidden">{children}</main>
            </div>
            <Footer />
        </div>
    );
};

const Footer = () => {
    return <footer className="flex h-8 items-center justify-center bg-slate-500"></footer>;
};
