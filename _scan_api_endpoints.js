// Temporary script to scan API endpoints
// This file will be deleted after scanning

const fs = require('fs');
const path = require('path');

const projectRoot = 'D:/WorkSpace/Demif-BE';

function scanDirectory(dir, extensions = ['.js', '.ts', '.java', '.py', '.go', '.cs', '.php']) {
    let files = [];
    try {
        const items = fs.readdirSync(dir);
        for (const item of items) {
            const fullPath = path.join(dir, item);
            const stat = fs.statSync(fullPath);
            if (stat.isDirectory() && !item.startsWith('.') && item !== 'node_modules' && item !== 'dist' && item !== 'build' && item !== 'vendor') {
                files = files.concat(scanDirectory(fullPath, extensions));
            } else if (stat.isFile() && extensions.some(ext => item.endsWith(ext))) {
                files.push(fullPath);
            }
        }
    } catch (e) {}
    return files;
}

const files = scanDirectory(projectRoot);
console.log(JSON.stringify(files, null, 2));
