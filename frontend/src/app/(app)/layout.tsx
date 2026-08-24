import type { Metadata } from "next";
import type { ReactNode } from "react";
import Image from "next/image";
import Link from "next/link";
import { Geist, Geist_Mono } from "next/font/google";
import { CurrentUserProvider } from "@/components/current-user-provider";
import NavigationUser from "@/components/navigation-user";
import "../globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Book Illustration Studio",
  description: "Create illustrated book projects with Gradion.",
};

export default function AppLayout({ children }: { children: ReactNode }) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <CurrentUserProvider>
          <header className="app-header sticky top-0 z-10">
            <nav className="mx-auto flex max-w-[1100px] items-center gap-7 px-8 py-3.5">
              <Link href="/projects" className="app-nav-logo">
                <Image
                  src="/gradion-logo.png"
                  alt="Gradion"
                  width={93}
                  height={22}
                  priority
                />
              </Link>
              <Link href="/projects" className="app-nav-link text-sm font-medium">
                Projects
              </Link>
              <NavigationUser />
            </nav>
          </header>

          <main className="flex-1">{children}</main>

          <footer className="app-footer px-5 py-12 text-center text-xs">
            GRADION <span className="mx-1">|</span>
            <strong className="font-semibold">Scaling Business</strong>
          </footer>
        </CurrentUserProvider>
      </body>
    </html>
  );
}
