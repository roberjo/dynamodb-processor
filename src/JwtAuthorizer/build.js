import { execSync } from 'child_process';
import { mkdir, copyFile } from 'fs/promises';
import { join } from 'path';

async function build() {
  try {
    // Create dist directory
    await mkdir('dist', { recursive: true });

    // Copy package.json and install production dependencies
    await copyFile('package.json', join('dist', 'package.json'));
    execSync('npm install --production', { cwd: 'dist', stdio: 'inherit' });

    // Copy source files
    await copyFile('index.js', join('dist', 'index.js'));

    console.log('Build completed successfully!');
  } catch (error) {
    console.error('Build failed:', error);
    process.exit(1);
  }
}

build(); 